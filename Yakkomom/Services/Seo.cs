using System.Text.Json;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Services;

/// <summary>
/// Référencement : balises de partage et données structurées schema.org.
/// Le JSON est sérialisé avec l'encodeur par défaut, qui échappe &lt; &gt; &amp; :
/// aucun contenu ne peut refermer la balise &lt;script type="application/ld+json"&gt;.
/// </summary>
public class Seo(UrlSite urls, IAssetsStatiques assets, IStorageService stockage)
{
    public const string DevisePaiement = "XOF"; // code ISO 4217 du franc CFA (UEMOA)

    public ImagePartage ImageParDefaut(string nomSite) =>
        new(urls.Absolue(assets["img/partage/yakkomom-1200x630.jpg"]), 1200, 630, "image/jpeg", $"{nomSite} — terrains vérifiés au Sénégal");

    public ImagePartage ImagePhoto(TerrainPhoto p, string alt)
    {
        var (url, l, h, type) = stockage.ImagePartage(p.CleStockage, p.Largeur, p.Hauteur, p.TypeMime);
        return new ImagePartage(urls.Absolue(url), l, h, type, alt);
    }

    public ImagePartage ImagePhoto(ServicePhoto p, string alt)
    {
        var (url, l, h, type) = stockage.ImagePartage(p.CleStockage, p.Largeur, p.Hauteur, p.TypeMime);
        return new ImagePartage(urls.Absolue(url), l, h, type, alt);
    }

    // =====================================================================
    // Données structurées
    // =====================================================================

    /// <summary>L'agence et le site (page d'accueil).</summary>
    public IEnumerable<string> Organisation(ParametreSite p)
    {
        var sameAs = new[] { p.Facebook, p.Instagram, p.TikTok, p.YouTube, p.LinkedIn }.Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();
        var telephone = p.ContactsParDefaut.WhatsApp1;
        yield return Json(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "RealEstateAgent",
            ["@id"] = urls.Absolue("/#organisation"),
            ["name"] = p.NomSite,
            ["description"] = p.Slogan,
            ["url"] = urls.Absolue("/"),
            ["logo"] = urls.Absolue(assets["img/icones/icone-512.png"]),
            ["image"] = urls.Absolue(assets["img/partage/yakkomom-1200x630.jpg"]),
            ["telephone"] = telephone,
            ["email"] = p.ContactsParDefaut.Email,
            ["address"] = string.IsNullOrWhiteSpace(p.Adresse) ? null
                : new Dictionary<string, object> { ["@type"] = "PostalAddress", ["streetAddress"] = p.Adresse, ["addressCountry"] = "SN" },
            ["areaServed"] = Pays,
            ["sameAs"] = sameAs.Length > 0 ? sameAs : null
        });
        yield return Json(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = p.NomSite,
            ["url"] = urls.Absolue("/"),
            ["inLanguage"] = "fr-SN",
            ["publisher"] = new Dictionary<string, object> { ["@id"] = urls.Absolue("/#organisation") },
            ["potentialAction"] = new Dictionary<string, object>
            {
                ["@type"] = "SearchAction",
                ["target"] = new Dictionary<string, object> { ["@type"] = "EntryPoint", ["urlTemplate"] = urls.Absolue("/terrains?q={search_term_string}") },
                ["query-input"] = "required name=search_term_string"
            }
        });
    }

    /// <summary>Annonce de terrain : RealEstateListing + offre (prix en XOF, disponibilité).</summary>
    public string Annonce(FicheTerrainVm f, IEnumerable<string> images, DateTime? publieLe)
    {
        var disponibilite = f.Statut switch
        {
            StatutTerrain.Disponible => "https://schema.org/InStock",
            StatutTerrain.Reserve => "https://schema.org/LimitedAvailability",
            _ => "https://schema.org/SoldOut"
        };
        var lieu = new Dictionary<string, object?>
        {
            ["@type"] = "Place",
            ["name"] = f.Localisation,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = f.Commune ?? f.QuartierVillage,
                ["addressRegion"] = f.Region,
                ["addressCountry"] = "SN"
            },
            ["geo"] = f.Latitude is { } lat && f.Longitude is { } lng
                ? new Dictionary<string, object> { ["@type"] = "GeoCoordinates", ["latitude"] = Math.Round(lat, 5), ["longitude"] = Math.Round(lng, 5) }
                : null
        };
        var proprietes = new List<object>();
        if (f.SurfaceM2 is { } s) proprietes.Add(new Dictionary<string, object> { ["@type"] = "PropertyValue", ["name"] = "Surface", ["value"] = s, ["unitCode"] = "MTK" });
        if (f.SituationFonciere is { } sf) proprietes.Add(new Dictionary<string, object> { ["@type"] = "PropertyValue", ["name"] = "Document foncier", ["value"] = TerrainPublicService.NomType(sf) });

        return Json(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "RealEstateListing",
            ["@id"] = f.UrlCanonique,
            ["url"] = f.UrlCanonique,
            ["name"] = f.Titre,
            ["description"] = Tronquer(f.Description ?? f.Resume, 500),
            ["identifier"] = f.Reference,
            ["image"] = images.ToArray() is { Length: > 0 } imgs ? imgs : null,
            ["datePosted"] = publieLe?.ToString("yyyy-MM-dd"),
            ["dateModified"] = f.ModifieLe.ToString("yyyy-MM-dd"),
            ["about"] = lieu,
            ["additionalProperty"] = proprietes.Count > 0 ? proprietes : null,
            ["offers"] = f.Prix is { } prix
                ? new Dictionary<string, object?>
                {
                    ["@type"] = "Offer",
                    ["price"] = prix,
                    ["priceCurrency"] = DevisePaiement,
                    ["availability"] = disponibilite,
                    ["url"] = f.UrlCanonique,
                    ["seller"] = new Dictionary<string, object> { ["@id"] = urls.Absolue("/#organisation") }
                }
                : null
        });
    }

    public string Service(ServicePublicVm s, string nomSite) => Json(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Service",
        ["name"] = s.Titre,
        ["description"] = Tronquer(s.Resume ?? s.Description, 300),
        ["url"] = urls.Absolue($"/services/{s.Slug}"),
        ["areaServed"] = Pays,
        ["provider"] = new Dictionary<string, object> { ["@type"] = "RealEstateAgent", ["@id"] = urls.Absolue("/#organisation"), ["name"] = nomSite }
    });

    /// <summary>Fil d'Ariane : [(nom, chemin)] depuis l'accueil.</summary>
    public string FilAriane(params (string Nom, string Chemin)[] etapes) => Json(new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = etapes.Select((e, i) => new Dictionary<string, object>
        {
            ["@type"] = "ListItem", ["position"] = i + 1, ["name"] = e.Nom, ["item"] = urls.Absolue(e.Chemin)
        }).ToArray()
    });

    private static readonly Dictionary<string, object> Pays = new() { ["@type"] = "Country", ["name"] = "Sénégal" };

    private static readonly JsonSerializerOptions Options = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    // Pas d'encodeur « relaxed » ici : <, >, & restent échappés (<…), le JSON ne peut pas fermer la balise script.
    private static string Json(object valeur) => JsonSerializer.Serialize(valeur, Options);

    public static string? Tronquer(string? texte, int max)
    {
        if (string.IsNullOrWhiteSpace(texte)) return null;
        var t = string.Join(' ', texte.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return t.Length <= max ? t : t[..(t.LastIndexOf(' ', max - 1) is > 0 and var i ? i : max - 1)] + "…";
    }
}
