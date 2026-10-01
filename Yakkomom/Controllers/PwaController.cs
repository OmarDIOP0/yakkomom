using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Yakkomom.Helpers;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>Application installable : service worker, manifeste, page hors ligne, mode économie de données.</summary>
public class PwaController(IAssetsStatiques assets, IParametreSiteService parametres, IWebHostEnvironment env) : Controller
{
    /// <summary>Fichiers mis en cache dès l'installation (légers, utilisés sur toutes les pages publiques).</summary>
    private static readonly string[] Precache =
    [
        "css/site.css", "js/site.js", "js/pwa.js", "js/chargeur.js", "js/carte/leaflet.js",
        "js/public/fiche.js", "js/public/filtres.js", "js/public/favoris-page.js", "js/public/hors-ligne.js",
        "fonts/fraunces-latin.woff2", "img/icons.svg", "img/logo.svg", "img/icones/icone-192.png"
    ];

    private static string? _script;

    /// <summary>
    /// Service worker à la racine (portée « / »). Sa version dépend des empreintes des fichiers :
    /// chaque déploiement qui modifie un fichier installe automatiquement la nouvelle version.
    /// </summary>
    [HttpGet("sw.js")]
    public IActionResult ServiceWorker()
    {
        if (_script is null || env.IsDevelopment())
        {
            var urls = Precache.Select(p => assets[p]).ToArray();
            var corps = System.IO.File.ReadAllText(Path.Combine(env.ContentRootPath, "Pwa", "sw.js"));
            var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', urls) + corps)))[..12].ToLowerInvariant();
            _script = $"const VERSION = '{version}';\nconst PRECACHE = {JsonSerializer.Serialize(urls)};\n\n{corps}";
        }
        Response.Headers.CacheControl = "no-cache";
        return Content(_script, "text/javascript; charset=utf-8");
    }

    [HttpGet("manifest.webmanifest")]
    public async Task<IActionResult> Manifeste(CancellationToken ct)
    {
        var p = await parametres.ObtenirAsync(ct);
        object Icone(string chemin, int taille, string usage = "any") =>
            new { src = assets[chemin], sizes = $"{taille}x{taille}", type = "image/png", purpose = usage };

        var manifeste = new
        {
            id = "/",
            name = $"{p.NomSite} — Terrains vérifiés au Sénégal",
            short_name = p.NomSite,
            description = p.Slogan ?? "Terrains vérifiés et services immobiliers au Sénégal : photos, visite 360°, carte et contact WhatsApp.",
            lang = "fr",
            dir = "ltr",
            start_url = "/?source=application",
            scope = "/",
            display = "standalone",
            orientation = "portrait",
            background_color = "#FBF7F0",
            theme_color = "#1C2A21",
            categories = new[] { "business", "lifestyle" },
            icons = new[]
            {
                Icone("img/icones/icone-72.png", 72), Icone("img/icones/icone-96.png", 96), Icone("img/icones/icone-128.png", 128),
                Icone("img/icones/icone-144.png", 144), Icone("img/icones/icone-152.png", 152), Icone("img/icones/icone-192.png", 192),
                Icone("img/icones/icone-384.png", 384), Icone("img/icones/icone-512.png", 512),
                Icone("img/icones/maskable-192.png", 192, "maskable"), Icone("img/icones/maskable-512.png", 512, "maskable")
            },
            screenshots = new object[]
            {
                new { src = assets["img/captures/accueil-mobile.png"], sizes = "390x844", type = "image/png", form_factor = "narrow", label = "Accueil : terrains vérifiés" },
                new { src = assets["img/captures/fiche-mobile.png"], sizes = "390x844", type = "image/png", form_factor = "narrow", label = "Fiche terrain : photos, prix, contact WhatsApp" },
                new { src = assets["img/captures/liste-bureau.png"], sizes = "1280x800", type = "image/png", form_factor = "wide", label = "Liste des terrains avec filtres" }
            },
            shortcuts = new[]
            {
                new { name = "Terrains", short_name = "Terrains", description = "Voir les terrains disponibles", url = "/terrains?source=raccourci",
                      icons = new[] { new { src = assets["img/icones/raccourci-terrains.png"], sizes = "96x96", type = "image/png" } } },
                new { name = "Services", short_name = "Services", description = "Construction, accompagnement, sécurisation", url = "/services?source=raccourci",
                      icons = new[] { new { src = assets["img/icones/raccourci-services.png"], sizes = "96x96", type = "image/png" } } },
                new { name = "Mes favoris", short_name = "Favoris", description = "Les terrains mis de côté", url = "/favoris?source=raccourci",
                      icons = new[] { new { src = assets["img/icones/raccourci-favoris.png"], sizes = "96x96", type = "image/png" } } }
            }
        };

        Response.Headers.CacheControl = "public, max-age=86400";
        return Content(JsonSerializer.Serialize(manifeste, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }),
            "application/manifest+json; charset=utf-8");
    }

    /// <summary>
    /// Seconde application, réservée à l'équipe : portée limitée à /admin, ouverture sur le tableau de bord.
    /// Public (comme le manifeste du site) : il ne contient aucune donnée, seulement le nom et les icônes,
    /// et la page de connexion doit pouvoir le lire.
    /// </summary>
    [HttpGet("admin/manifest.webmanifest")]
    public async Task<IActionResult> ManifesteAdmin(CancellationToken ct)
    {
        var p = await parametres.ObtenirAsync(ct);
        object Icone(string chemin, int taille, string usage = "any") =>
            new { src = assets[chemin], sizes = $"{taille}x{taille}", type = "image/png", purpose = usage };
        object Raccourci(string nom, string url, string? icone = null) => icone is null
            ? new { name = nom, short_name = nom, url }
            : new { name = nom, short_name = nom, url, icons = new[] { new { src = assets[icone], sizes = "96x96", type = "image/png" } } };

        var manifeste = new
        {
            id = "/admin",
            name = $"{p.NomSite} Admin",
            short_name = $"{p.NomSite} Admin",
            description = "Gestion des terrains, photos, documents et statistiques WhatsApp.",
            lang = "fr",
            dir = "ltr",
            start_url = "/admin?source=application",
            scope = "/admin",
            display = "standalone",
            orientation = "portrait",
            background_color = "#FBF7F0",
            theme_color = "#FBF7F0",
            icons = new[]
            {
                Icone("img/icones/admin-192.png", 192), Icone("img/icones/admin-512.png", 512),
                Icone("img/icones/admin-maskable-192.png", 192, "maskable"), Icone("img/icones/admin-maskable-512.png", 512, "maskable")
            },
            shortcuts = new[]
            {
                Raccourci("Nouveau terrain", "/admin/terrains/nouveau"),
                Raccourci("Terrains", "/admin/terrains", "img/icones/raccourci-terrains.png"),
                Raccourci("Journal", "/admin/journal")
            }
        };

        Response.Headers.CacheControl = "public, max-age=86400";
        return Content(JsonSerializer.Serialize(manifeste, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }),
            "application/manifest+json; charset=utf-8");
    }

    [HttpGet("hors-ligne")]
    public IActionResult HorsLigne()
    {
        ViewData["Rubrique"] = "horsligne";
        return View("~/Views/Pages/HorsLigne.cshtml");
    }

    /// <summary>Active ou désactive le mode économie de données (fonctionne sans JavaScript).</summary>
    [HttpGet("preferences/economie")]
    public IActionResult Economie(bool actif, string? retour)
    {
        if (actif)
            Response.Cookies.Append(ModeEconomie.Cookie, "1", new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365), HttpOnly = false, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps, IsEssential = true
            });
        else
            Response.Cookies.Delete(ModeEconomie.Cookie);

        Response.Headers.CacheControl = "no-store";
        return LocalRedirect(!string.IsNullOrEmpty(retour) && Url.IsLocalUrl(retour) ? retour : "/");
    }
}
