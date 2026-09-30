using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services;
using Yakkomom.Stockage;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Photos et documents fonciers d'un terrain.</summary>
public partial class TerrainsController
{
    // =====================================================================
    // Photos
    // =====================================================================

    /// <summary>Envoi d'une photo déjà compressée par le navigateur (réponse JSON pour la barre de progression).</summary>
    [HttpPost("{id:int}/photos/envoi")]
    [RequestSizeLimit(ReglesEnvoi.RequetePhotoMax)]
    [RequestFormLimits(MultipartBodyLengthLimit = ReglesEnvoi.RequetePhotoMax)]
    public async Task<IActionResult> EnvoyerPhoto(int id, IFormFile? fichier, IFormFile? vignette, [FromForm] EnvoiPhoto infos, CancellationToken ct)
    {
        var r = await medias.AjouterPhotoAsync(id, fichier, vignette, infos, ct);
        return r.Reussi ? Json(r.Valeur) : BadRequest(new { erreur = r.Erreur });
    }

    public record OrdrePhotos(int[] Ids);

    /// <summary>Nouvel ordre après glisser-déposer (JSON).</summary>
    [HttpPost("{id:int}/photos/ordre")]
    public async Task<IActionResult> OrdonnerPhotos(int id, [FromBody] OrdrePhotos ordre, CancellationToken ct)
    {
        var r = await medias.OrdonnerPhotosAsync(id, ordre.Ids ?? [], ct);
        return r.Reussi ? NoContent() : BadRequest(new { erreur = r.Erreur });
    }

    [HttpPost("{id:int}/photos/{photoId:int}/couverture")]
    public async Task<IActionResult> CouverturePhoto(int id, int photoId, CancellationToken ct) =>
        ApresAction(await medias.DefinirCouvertureAsync(id, photoId, ct), "Photo de couverture modifiée.", nameof(Photos), id);

    [HttpPost("{id:int}/photos/{photoId:int}/legende")]
    public async Task<IActionResult> LegendePhoto(int id, int photoId, string? legende, CancellationToken ct)
    {
        var r = await medias.ModifierLegendeAsync(id, photoId, legende, ct);
        if (EstRequeteScript) return r.Reussi ? NoContent() : BadRequest(new { erreur = r.Erreur });
        return ApresAction(r, "Légende enregistrée.", nameof(Photos), id);
    }

    [HttpPost("{id:int}/photos/{photoId:int}/deplacer")]
    public async Task<IActionResult> DeplacerPhoto(int id, int photoId, int decalage, CancellationToken ct) =>
        ApresAction(await medias.DeplacerPhotoAsync(id, photoId, decalage, ct), null, nameof(Photos), id);

    [HttpPost("{id:int}/photos/{photoId:int}/supprimer")]
    public async Task<IActionResult> SupprimerPhoto(int id, int photoId, CancellationToken ct) =>
        ApresAction(await medias.SupprimerPhotoAsync(id, photoId, ct), "Photo supprimée.", nameof(Photos), id);

    // =====================================================================
    // Documents fonciers
    // =====================================================================
    [HttpPost("{id:int}/documents/envoi")]
    [RequestSizeLimit(ReglesEnvoi.RequeteDocumentMax)]
    [RequestFormLimits(MultipartBodyLengthLimit = ReglesEnvoi.RequeteDocumentMax)]
    public async Task<IActionResult> EnvoyerDocument(int id, IFormFile? fichier, [FromForm] EnvoiDocument infos, CancellationToken ct)
    {
        var r = await medias.AjouterDocumentAsync(id, fichier, infos, ct);
        if (EstRequeteScript) return r.Reussi ? Json(r.Valeur) : BadRequest(new { erreur = r.Erreur });
        return ApresAction(r.Reussi ? ResultatOperation.Ok() : ResultatOperation.Echec(r.Erreur!), "Document ajouté.", nameof(Documents), id);
    }

    [HttpPost("{id:int}/documents/{documentId:int}/visibilite")]
    public async Task<IActionResult> VisibiliteDocument(int id, int documentId, CancellationToken ct) =>
        ApresAction(await medias.BasculerVisibiliteDocumentAsync(id, documentId, ct), "Visibilité du document modifiée.", nameof(Documents), id);

    [HttpPost("{id:int}/documents/{documentId:int}/supprimer")]
    public async Task<IActionResult> SupprimerDocument(int id, int documentId, CancellationToken ct) =>
        ApresAction(await medias.SupprimerDocumentAsync(id, documentId, ct), "Document supprimé.", nameof(Documents), id);

    /// <summary>Consultation d'un document (public ou privé) par un admin connecté.</summary>
    [HttpGet("~/admin/documents/{documentId:int}")]
    public async Task<IActionResult> OuvrirDocument(int documentId, CancellationToken ct)
    {
        var ouvert = await medias.OuvrirDocumentAsync(documentId, accesAdmin: true, ct);
        return ouvert is null ? NotFound() : ServirDocument.Reponse(this, ouvert.Value.Document, ouvert.Value.Fichier);
    }

    // =====================================================================
    private bool EstRequeteScript => Request.Headers.Accept.ToString().Contains("application/json");

    private IActionResult ApresAction(ResultatOperation r, string? succes, string onglet, int id)
    {
        if (!r.Reussi) Erreur(r.Erreur ?? "Opération impossible.");
        else if (succes is not null) Succes(succes);
        return RedirectToAction(onglet, new { id });
    }
}
