using Microsoft.AspNetCore.Mvc;
using Yakkomom.Helpers;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>Favoris : stockés sur le téléphone (localStorage), sans compte.</summary>
public class FavorisController(ITerrainPublicService terrains) : Controller
{
    [HttpGet("favoris")]
    public IActionResult Index()
    {
        ViewData["Rubrique"] = "favoris";
        return View();
    }

    /// <summary>Données à jour des terrains favoris (statut, prix) : /api/terrains/cartes?refs=YK-0001,YK-0002</summary>
    [HttpGet("api/terrains/cartes")]
    public async Task<IActionResult> Cartes(string? refs, CancellationToken ct)
    {
        var liste = (refs ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cartes = await terrains.CartesAsync(liste, ct);
        Response.Headers.CacheControl = "no-cache";
        return Json(cartes.Select(c => new
        {
            reference = c.Reference, titre = c.Titre, url = c.Url, statut = Format.Statut(c.Statut), vendu = c.Statut == Models.Enums.StatutTerrain.Vendu,
            prix = c.Prix is null ? null : Format.Montant(c.Prix.Value), surface = Format.Surface(c.SurfaceM2), lieu = c.Localisation,
            image = c.ImageUrl, couleur = c.CouleurDominante
        }));
    }
}
