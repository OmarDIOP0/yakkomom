using Microsoft.AspNetCore.Mvc;
using Yakkomom.Helpers;
using Yakkomom.Models.ViewModels;
using Yakkomom.Securite;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>Terrains côté public : liste filtrée et fiche détaillée.</summary>
[Route("terrains")]
public class TerrainsController(ITerrainPublicService terrains, Yakkomom.Services.Seo seo, Yakkomom.Services.UrlSite urls) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] FiltreTerrainsPublic filtre, CancellationToken ct)
    {
        ViewData["Rubrique"] = "terrains";
        var liste = await terrains.ListerAsync(filtre, ct);

        // Une seule version indexée de la liste (pagination comprise) ; les combinaisons de filtres ne le sont pas.
        var filtree = filtre.NombreFiltresActifs > 0 || !string.IsNullOrWhiteSpace(filtre.Q) || !string.IsNullOrEmpty(filtre.Tri);
        ViewData[MetaPage.Cle] = new MetaPage
        {
            Titre = liste.Page > 1 ? $"Terrains à vendre au Sénégal — page {liste.Page}" : "Terrains à vendre au Sénégal",
            Canonique = urls.Absolue(liste.Page > 1 ? $"/terrains?page={liste.Page}" : "/terrains"),
            Robots = filtree ? "noindex, follow" : null
        };
        return View(liste);
    }

    /// <summary>/terrains/yk-0042-terrain-500m2-nguekokh (seule la référence compte ; le reste est redirigé).</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Fiche(string slug, CancellationToken ct)
    {
        var reference = ReferenceTerrain.DepuisSlug(slug);
        if (reference is null) return NotFound();

        var estAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);
        var fiche = await terrains.FicheAsync(reference, apercuAdmin: estAdmin, ct);
        if (fiche is null) return NotFound();

        // Une seule URL par terrain (titre, surface ou commune modifiés → ancienne URL redirigée)
        var canonique = new Uri(fiche.UrlCanonique).AbsolutePath;
        if (!string.Equals(Request.Path.Value, canonique, StringComparison.Ordinal))
            return RedirectPermanent(canonique + Request.QueryString);

        if (fiche.EstApercuAdmin) Response.Headers["X-Robots-Tag"] = "noindex";
        ViewData["Rubrique"] = "terrains";

        var prix = fiche.Prix is null ? "prix sur demande" : Format.Fcfa(fiche.Prix);
        var atouts = new List<string>();
        if (fiche.AUnTitreFoncier) atouts.Add("titre foncier");
        if (fiche.Visite is not null) atouts.Add("visite 360°");
        if (fiche.ContourGeoJson is not null) atouts.Add("parcelle géolocalisée");
        var meta = new MetaPage
        {
            // Ce qui s'affiche dans l'aperçu WhatsApp : photo, titre + prix, localisation
            Titre = $"{fiche.Titre} — {prix}",
            Description = string.Join(" · ", new[]
            {
                fiche.Statut == Models.Enums.StatutTerrain.Vendu ? "VENDU" : fiche.Statut == Models.Enums.StatutTerrain.Reserve ? "Réservé" : null,
                fiche.Resume, fiche.Localisation, atouts.Count > 0 ? string.Join(", ", atouts) : null, $"Réf. {fiche.Reference}"
            }.Where(x => !string.IsNullOrWhiteSpace(x))) + ". Photos, carte et contact WhatsApp.",
            Canonique = fiche.UrlCanonique,
            Image = fiche.ImagePartage,
            Type = "product",
            PrixFcfa = fiche.Prix,
            Robots = fiche.EstApercuAdmin ? "noindex" : null
        };
        meta.DonneesStructurees.Add(seo.Annonce(fiche, fiche.Photos.Take(6).Select(p => urls.Absolue(p.UrlGrande)), fiche.PublieLe));
        var ariane = new List<(string, string)> { ("Accueil", "/"), ("Terrains", "/terrains") };
        if (fiche.Commune is not null) ariane.Add((fiche.Commune, $"/terrains?q={Uri.EscapeDataString(fiche.Commune)}"));
        ariane.Add((fiche.Titre, new Uri(fiche.UrlCanonique).AbsolutePath));
        meta.DonneesStructurees.Add(seo.FilAriane([.. ariane]));
        ViewData[MetaPage.Cle] = meta;
        return View(fiche);
    }
}
