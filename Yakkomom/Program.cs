using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Data.Seed;
using Yakkomom.Securite;
using Yakkomom.Services;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

var builder = WebApplication.CreateBuilder(args);

// Render (et la plupart des hébergeurs de conteneurs) imposent le port d'écoute via PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Derrière le proxy HTTPS de l'hébergeur, la redirection vise toujours le port 443.
if (!builder.Environment.IsDevelopment())
    builder.Services.AddHttpsRedirection(o => o.HttpsPort = 443);

// --- Base de données (PostgreSQL) ------------------------------------------
builder.Services.AddDbContext<YakkomomDbContext>(options =>
    options.UseNpgsql(ConnexionBaseDeDonnees.Lire(builder.Configuration),
            npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
        .UseSnakeCaseNamingConvention());
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<AdminInitialSeeder>();
builder.Services.AddScoped<DemoSeeder>();

// --- Sécurité : Identity, rôles, cookies, limitation de débit, proxy -------
builder.Services.AjouterSecuriteAdmin(builder.Environment);

// --- Services métier --------------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IParametreSiteService, ParametreSiteService>();
builder.Services.AddSingleton<IAssetsStatiques, AssetsStatiques>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IGestionComptesService, GestionComptesService>();
builder.Services.AddScoped<ILocaliteService, LocaliteService>();
builder.Services.AddScoped<ITerrainAdminService, TerrainAdminService>();
builder.Services.AddScoped<ITableauDeBordService, TableauDeBordService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IVisite360Service, Visite360Service>();
builder.Services.AddScoped<IServicesSiteService, ServicesSiteService>();
builder.Services.AddScoped<ITerrainPublicService, TerrainPublicService>();
builder.Services.AddScoped<ImagesPubliques>();
builder.Services.AddScoped<UrlSite>();
builder.Services.AddScoped<Seo>();

// --- Stockage des fichiers : disque en développement, Cloudinary en production ----
builder.Services.Configure<OptionsStockage>(builder.Configuration.GetSection(OptionsStockage.Section));
var fournisseurStockage = builder.Configuration[$"{OptionsStockage.Section}:Fournisseur"] ?? "Local";
if (fournisseurStockage.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IStorageService, StockageCloudinary>();
else
{
    builder.Services.AddSingleton<StockageLocal>();
    builder.Services.AddSingleton<IStorageService>(sp => sp.GetRequiredService<StockageLocal>());
}

// --- Compression des réponses dynamiques (HTML, JSON) ------------------------
// Les fichiers statiques sont déjà pré-compressés (Brotli + Gzip) par MapStaticAssets.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml", "application/manifest+json"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddControllersWithViews()
    // JSON lisible et plus léger : « é » plutôt que « é »
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        // Énumérations en texte (« Navigation » plutôt que 0) : plus lisible et robuste
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Accents écrits tels quels dans le HTML (« é » et non « &#xE9; ») : plus lisible et plus léger.
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

// --- Migrations + données de départ au démarrage ---------------------------
if (app.Configuration.GetValue("Database:MigrerAuDemarrage", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<YakkomomDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<AdminInitialSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync();
}

// --- Pipeline HTTP ---------------------------------------------------------
app.UseForwardedHeaders(); // en premier : vraie IP et schéma HTTPS derrière Render

// Site de démonstration ou de recette : rien n'est indexé par les moteurs de recherche.
if (!app.Configuration.GetValue("Site:Indexable", true))
    app.Use((contexte, suivant) =>
    {
        contexte.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        return suivant(contexte);
    });

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/erreur/{0}");
// Pas de redirection HTTPS pour /health : les sondes internes de l'hébergeur appellent en HTTP.
app.UseWhen(c => !c.Request.Path.StartsWithSegments("/health"), a => a.UseHttpsRedirection());
app.UseResponseCompression();
// Fichiers publics du stockage local (photos, logo) : noms uniques, donc cache immuable.
// Les documents privés (App_Data/prive) ne sont JAMAIS servis ici.
if (app.Services.GetRequiredService<IStorageService>() is StockageLocal stockageLocal)
{
    if (!app.Environment.IsDevelopment())
        app.Logger.LogWarning("Stockage LOCAL en production : les fichiers seront perdus au redémarrage sur Render. Configurez Cloudinary.");
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(stockageLocal.RacinePublique),
        RequestPath = StockageLocal.CheminPublic,
        ServeUnknownFileTypes = false,
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
        }
    });
}

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Vérification de santé (Render, UptimeRobot) : volontairement sans accès à la base,
// pour ne pas réveiller inutilement une base gratuite mise en veille.
app.MapGet("/health", (HttpContext c) =>
{
    c.Response.Headers.CacheControl = "no-store";
    return Results.Text("ok");
}).ExcludeFromDescription();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
