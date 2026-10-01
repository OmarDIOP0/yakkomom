using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Enums;
using Yakkomom.Services;
using Yakkomom.Stockage;

namespace Yakkomom.Controllers;

/// <summary>robots.txt et sitemap.xml (générés à la volée).</summary>
public class SeoController(YakkomomDbContext db, UrlSite urls, IStorageService stockage, IMemoryCache cache, IConfiguration configuration) : Controller
{
    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        Response.Headers.CacheControl = "public, max-age=86400";
        if (!configuration.GetValue("Site:Indexable", true))
            return Content("# Site de démonstration : aucune indexation\nUser-agent: *\nDisallow: /\n", "text/plain; charset=utf-8");

        var texte = $"""
            # Yakkomom — consignes aux robots d'indexation
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /api/
            Disallow: /wa/
            Disallow: /documents/
            Disallow: /preferences/
            Disallow: /hors-ligne
            Disallow: /favoris

            Sitemap: {urls.Absolue("/sitemap.xml")}
            """;
        return Content(texte + "\n", "text/plain; charset=utf-8");
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var xml = await cache.GetOrCreateAsync("sitemap:" + urls.Base, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await ConstruireAsync(ct);
        });
        Response.Headers.CacheControl = "public, max-age=3600";
        return Content(xml!, "application/xml; charset=utf-8");
    }

    private async Task<string> ConstruireAsync(CancellationToken ct)
    {
        var terrains = await db.Terrains.AsNoTracking()
            .Where(t => t.Statut == StatutTerrain.Disponible || t.Statut == StatutTerrain.Reserve || t.Statut == StatutTerrain.Vendu)
            .OrderByDescending(t => t.ModifieLe)
            .Select(t => new
            {
                t.Reference, t.Titre, t.SurfaceM2, t.ModifieLe, t.Statut,
                Commune = t.Commune != null ? t.Commune.Nom : null,
                Couverture = t.Photos.OrderByDescending(p => p.EstCouverture).ThenBy(p => p.Ordre).Select(p => p.CleStockage).FirstOrDefault()
            }).ToListAsync(ct);
        var services = await db.Services.AsNoTracking().Where(s => s.EstActif).OrderBy(s => s.Ordre)
            .Select(s => new { s.Slug, s.ModifieLe }).ToListAsync(ct);
        var derniereMaj = terrains.Select(t => t.ModifieLe).DefaultIfEmpty(DateTime.UtcNow).Max();

        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
        {
            const string ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            const string nsImage = "http://www.google.com/schemas/sitemap-image/1.1";
            w.WriteStartDocument();
            w.WriteStartElement("urlset", ns);
            w.WriteAttributeString("xmlns", "image", null, nsImage);

            void Url(string chemin, DateTime? modifie, string frequence, string priorite, string? image = null, string? titreImage = null)
            {
                w.WriteStartElement("url", ns);
                w.WriteElementString("loc", ns, urls.Absolue(chemin));
                if (modifie is { } m) w.WriteElementString("lastmod", ns, m.ToString("yyyy-MM-dd"));
                w.WriteElementString("changefreq", ns, frequence);
                w.WriteElementString("priority", ns, priorite);
                if (image is not null)
                {
                    w.WriteStartElement("image", "image", nsImage);
                    w.WriteElementString("image", "loc", nsImage, urls.Absolue(image));
                    if (titreImage is not null) w.WriteElementString("image", "title", nsImage, titreImage);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }

            Url("/", derniereMaj, "daily", "1.0");
            Url("/terrains", derniereMaj, "daily", "0.9");
            Url("/services", null, "monthly", "0.7");
            foreach (var s in services) Url($"/services/{s.Slug}", s.ModifieLe, "monthly", "0.6");
            Url("/acheter-en-securite", null, "yearly", "0.6");
            Url("/a-propos", null, "yearly", "0.4");
            Url("/contact", null, "yearly", "0.5");
            Url("/mentions-legales", null, "yearly", "0.2");
            Url("/conditions-utilisation", null, "yearly", "0.2");
            foreach (var t in terrains)
                Url(UrlTerrain.Chemin(t.Reference, t.SurfaceM2, t.Commune), t.ModifieLe,
                    t.Statut == StatutTerrain.Vendu ? "monthly" : "weekly", t.Statut == StatutTerrain.Vendu ? "0.3" : "0.8",
                    t.Couverture is null ? null : stockage.UrlImage(t.Couverture, 1200), t.Titre);

            w.WriteEndElement();
            w.WriteEndDocument();
        }
        return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
    }
}
