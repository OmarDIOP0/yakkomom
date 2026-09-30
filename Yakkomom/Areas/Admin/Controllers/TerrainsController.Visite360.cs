using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.ViewModels;
using Yakkomom.Services;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Visite 360° : panoramas, vue de départ et points de passage.</summary>
public partial class TerrainsController
{
    private const long RequetePanoramaMax = 9 * 1024 * 1024;

    [HttpPost("{id:int}/panoramas/envoi")]
    [RequestSizeLimit(RequetePanoramaMax)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequetePanoramaMax)]
    public async Task<IActionResult> EnvoyerPanorama(int id, IFormFile? hd, IFormFile? bd, IFormFile? vignette, [FromForm] EnvoiPanorama infos, CancellationToken ct)
    {
        var r = await visite360.AjouterAsync(id, hd, bd, vignette, infos, ct);
        return r.Reussi ? Json(r.Valeur) : BadRequest(new { erreur = r.Erreur });
    }

    [HttpPost("{id:int}/panoramas/{panoramaId:int}/titre")]
    public async Task<IActionResult> TitrePanorama(int id, int panoramaId, string? titre, CancellationToken ct) =>
        ApresAction(await visite360.RenommerAsync(id, panoramaId, titre, ct), "Titre enregistré.", nameof(Visite360), id);

    [HttpPost("{id:int}/panoramas/{panoramaId:int}/depart")]
    public async Task<IActionResult> DepartPanorama(int id, int panoramaId, CancellationToken ct) =>
        ApresAction(await visite360.DefinirDepartAsync(id, panoramaId, ct), "Scène de départ modifiée.", nameof(Visite360), id);

    [HttpPost("{id:int}/panoramas/{panoramaId:int}/deplacer")]
    public async Task<IActionResult> DeplacerPanorama(int id, int panoramaId, int decalage, CancellationToken ct) =>
        ApresAction(await visite360.DeplacerAsync(id, panoramaId, decalage, ct), null, nameof(Visite360), id);

    [HttpPost("{id:int}/panoramas/{panoramaId:int}/supprimer")]
    public async Task<IActionResult> SupprimerPanorama(int id, int panoramaId, CancellationToken ct) =>
        ApresAction(await visite360.SupprimerAsync(id, panoramaId, ct), "Panorama supprimé.", nameof(Visite360), id);

    // --- Éditeur (JSON) ----------------------------------------------------
    [HttpPost("{id:int}/panoramas/{panoramaId:int}/vue")]
    public async Task<IActionResult> VuePanorama(int id, int panoramaId, [FromBody] VueInitiale vue, CancellationToken ct)
    {
        var r = await visite360.EnregistrerVueAsync(id, panoramaId, vue, ct);
        return r.Reussi ? NoContent() : BadRequest(new { erreur = r.Erreur });
    }

    [HttpPost("{id:int}/panoramas/{panoramaId:int}/hotspots")]
    public async Task<IActionResult> AjouterHotspot(int id, int panoramaId, [FromBody] NouveauHotspot hotspot, CancellationToken ct)
    {
        var r = await visite360.AjouterHotspotAsync(id, panoramaId, hotspot, ct);
        return r.Reussi ? Json(new { id = r.Valeur }) : BadRequest(new { erreur = r.Erreur });
    }

    [HttpPost("{id:int}/hotspots/{hotspotId:int}/supprimer")]
    public async Task<IActionResult> SupprimerHotspot(int id, int hotspotId, CancellationToken ct)
    {
        var r = await visite360.SupprimerHotspotAsync(id, hotspotId, ct);
        return r.Reussi ? NoContent() : BadRequest(new { erreur = r.Erreur });
    }
}
