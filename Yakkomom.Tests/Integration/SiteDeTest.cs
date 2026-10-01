using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Yakkomom.Data;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Stockage;

namespace Yakkomom.Tests.Integration;

/// <summary>
/// L'application complète en mémoire (environnement Production), sur une base PostgreSQL jetable
/// créée par les migrations et supprimée à la fin. Aucun secret dans le dépôt : la connexion au serveur
/// vient de la variable YK_TESTS_DB, ou à défaut des user-secrets de développement de l'application.
/// </summary>
public sealed partial class SiteDeTest : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _nomBase = "yk_tests_" + Guid.NewGuid().ToString("N")[..8];
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "yk-tests-" + Guid.NewGuid().ToString("N")[..8]);
    private readonly string _chaineServeur;

    public string EmailAdmin { get; } = "admin@tests.yakkomom.local";
    /// <summary>Mot de passe aléatoire, propre à chaque exécution.</summary>
    public string MotDePasseAdmin { get; } = "Test-" + Guid.NewGuid().ToString("N")[..12] + "-9";

    public Donnees D { get; private set; } = null!;

    public SiteDeTest()
    {
        var chaine = Environment.GetEnvironmentVariable("YK_TESTS_DB")
            ?? new ConfigurationBuilder().AddUserSecrets<Program>().Build().GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Tests d'intégration : définissez YK_TESTS_DB (chaîne Npgsql vers un serveur PostgreSQL de test) " +
                "ou la chaîne de développement dans les user-secrets de l'application.");
        _chaineServeur = chaine;

        // Variables lues pendant la construction de l'application (stockage, base) : posées avant son démarrage.
        var test = new NpgsqlConnectionStringBuilder(chaine) { Database = _nomBase, Pooling = false }.ConnectionString;
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", test);
        Environment.SetEnvironmentVariable("Stockage__Fournisseur", "Local");
        Environment.SetEnvironmentVariable("Stockage__DossierLocal", _dossier);
        Environment.SetEnvironmentVariable("AdminInitial__Email", EmailAdmin);
        Environment.SetEnvironmentVariable("AdminInitial__MotDePasse", MotDePasseAdmin);
        Environment.SetEnvironmentVariable("AdminInitial__Nom", "Admin Tests");
        Environment.SetEnvironmentVariable("Demo__Terrains", "false");
        Environment.SetEnvironmentVariable("Site__UrlPublique", "https://yakkomom.test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Production");

    /// <summary>
    /// Client HTTPS (comme en production derrière Render), sans suivre les redirections.
    /// Vrai nom de domaine : ASP.NET n'envoie jamais HSTS pour « localhost ».
    /// </summary>
    public HttpClient Client(bool navigateur = true)
    {
        var c = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://yakkomom.test"), AllowAutoRedirect = false });
        if (navigateur) c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/130.0 Mobile Safari/537.36");
        return c;
    }

    public async Task InitializeAsync()
    {
        // Les migrations tournent juste après l'ouverture du port : on attend la fin du démarrage.
        var client = Client();
        for (var i = 0; ; i++)
        {
            var r = await client.GetAsync("/");
            if (r.StatusCode != HttpStatusCode.ServiceUnavailable) break;
            if (i > 600) throw new TimeoutException("Démarrage de l'application de test trop long.");
            await Task.Delay(100);
        }
        D = await Donnees.CreerAsync(Services);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        var admin = new NpgsqlConnectionStringBuilder(_chaineServeur) { Database = "postgres", Pooling = false }.ConnectionString;
        await using (var cnx = new NpgsqlConnection(admin))
        {
            await cnx.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_nomBase}\" WITH (FORCE)", cnx);
            await cmd.ExecuteNonQueryAsync();
        }
        try { Directory.Delete(_dossier, recursive: true); } catch (IOException) { /* fichier encore ouvert : dossier temporaire */ }
    }

    /// <summary>Client connecté à l'espace admin avec le SuperAdmin de test.</summary>
    public async Task<HttpClient> ClientAdminAsync()
    {
        var c = Client();
        var page = await c.GetStringAsync("/admin/connexion");
        var reponse = await c.PostAsync("/admin/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = EmailAdmin,
            ["MotDePasse"] = MotDePasseAdmin,
            ["__RequestVerificationToken"] = Jeton(page)
        }));
        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Equal("/admin", reponse.Headers.Location?.OriginalString);
        return c;
    }

    public static string Jeton(string html) => JetonRegex().Match(html).Groups[1].Value;

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex JetonRegex();

    /// <summary>Jeu de données connu, créé une fois pour toute la série de tests.</summary>
    public sealed record Donnees(
        Terrain Abordable, Terrain Cher, Terrain Brouillon,
        DocumentFoncier DocPublic, DocumentFoncier DocPrive, DocumentFoncier DocPublicSurBrouillon)
    {
        public static async Task<Donnees> CreerAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YakkomomDbContext>();
            var stockage = scope.ServiceProvider.GetRequiredService<IStorageService>();

            Terrain Terrain(string reference, string titre, long prix, StatutTerrain statut) => new()
            {
                Reference = reference, Titre = titre, Prix = prix, SurfaceM2 = 500, Statut = statut,
                Type = TypeTerrain.Habitation, PublieLe = statut == StatutTerrain.Brouillon ? null : DateTime.UtcNow,
                Contacts = new Contact { WhatsApp1 = "+221700000001" } // numéro fictif
            };
            var abordable = Terrain("YK-9001", "Terrain test abordable", 10_000_000, StatutTerrain.Disponible);
            var cher = Terrain("YK-9002", "Terrain test cher", 25_000_000, StatutTerrain.Disponible);
            var brouillon = Terrain("YK-9003", "Terrain test brouillon", 5_000_000, StatutTerrain.Brouillon);
            db.Terrains.AddRange(abordable, cher, brouillon);
            await db.SaveChangesAsync();

            async Task<DocumentFoncier> Document(Terrain t, bool estPublic, string nom)
            {
                var cle = $"terrains/{t.Id}/documents/{Guid.NewGuid():N}.pdf";
                await using (var flux = new MemoryStream("%PDF-1.4\n% document de test\n"u8.ToArray()))
                    await stockage.EnregistrerAsync(flux, cle, "application/pdf", ZoneStockage.Privee);
                var d = new DocumentFoncier
                {
                    TerrainId = t.Id, Type = TypeDocumentFoncier.TitreFoncier, CleStockage = cle, NomFichierOriginal = nom,
                    TypeMime = "application/pdf", TailleOctets = 30, EstPublic = estPublic
                };
                db.DocumentsFonciers.Add(d);
                await db.SaveChangesAsync();
                return d;
            }

            return new Donnees(abordable, cher, brouillon,
                await Document(abordable, true, "plan-de-bornage.pdf"),
                await Document(abordable, false, "titre-foncier-confidentiel.pdf"),
                await Document(brouillon, true, "document-brouillon.pdf"));
        }
    }
}

[CollectionDefinition(Nom)]
public sealed class CollectionSite : ICollectionFixture<SiteDeTest>
{
    public const string Nom = "Site complet";
}
