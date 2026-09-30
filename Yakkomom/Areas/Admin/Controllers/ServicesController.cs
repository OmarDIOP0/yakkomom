using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.ViewModels;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Services : textes, icône, ordre d'affichage, visibilité et photos de réalisations.</summary>
[Route("admin/services")]
public class ServicesController(IServicesSiteService services) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await services.ListerAdminAsync(ct));

    [HttpPost("nouveau")]
    public async Task<IActionResult> Nouveau(NouveauServiceVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            Erreur("Donnez un titre au service.");
            return RedirectToAction(nameof(Index));
        }
        var id = await services.CreerAsync(vm.Titre, ct);
        Succes("Service créé (masqué). Complétez-le puis rendez-le visible.");
        return RedirectToAction(nameof(Modifier), new { id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Modifier(int id, CancellationToken ct)
    {
        var vm = await services.EditionAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("{id:int}")]
    public async Task<IActionResult> Modifier(int id, ServiceEditionVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (ModelState.IsValid)
        {
            var r = await services.EnregistrerAsync(vm, ct);
            if (r.Reussi)
            {
                Succes("Service enregistré.");
                return RedirectToAction(nameof(Modifier), new { id });
            }
            if (r.Erreur is not null) ModelState.AddModelError(string.Empty, r.Erreur);
            foreach (var (champ, message) in r.Erreurs) ModelState.AddModelError(champ, message);
        }
        var actuel = await services.EditionAsync(id, ct);
        if (actuel is null) return NotFound();
        vm.Slug = actuel.Slug;
        vm.Photos = actuel.Photos;
        return View(vm);
    }

    [HttpPost("{id:int}/deplacer")]
    public async Task<IActionResult> Deplacer(int id, int decalage, CancellationToken ct)
    {
        var r = await services.DeplacerAsync(id, decalage, ct);
        if (!r.Reussi) Erreur(r.Erreur!);
        return RedirectToAction(nameof(Index));
    }

    // --- Photos de réalisations -------------------------------------------
    [HttpPost("{id:int}/photos/envoi")]
    [RequestSizeLimit(ReglesEnvoi.RequetePhotoMax)]
    [RequestFormLimits(MultipartBodyLengthLimit = ReglesEnvoi.RequetePhotoMax)]
    public async Task<IActionResult> EnvoyerPhoto(int id, IFormFile? fichier, IFormFile? vignette, [FromForm] EnvoiPhoto infos, CancellationToken ct)
    {
        var r = await services.AjouterPhotoAsync(id, fichier, vignette, infos, ct);
        return r.Reussi ? Json(r.Valeur) : BadRequest(new { erreur = r.Erreur });
    }

    public record OrdrePhotos(int[] Ids);

    [HttpPost("{id:int}/photos/ordre")]
    public async Task<IActionResult> OrdonnerPhotos(int id, [FromBody] OrdrePhotos ordre, CancellationToken ct)
    {
        var r = await services.OrdonnerPhotosAsync(id, ordre.Ids ?? [], ct);
        return r.Reussi ? NoContent() : BadRequest(new { erreur = r.Erreur });
    }

    [HttpPost("{id:int}/photos/{photoId:int}/legende")]
    public async Task<IActionResult> LegendePhoto(int id, int photoId, string? legende, CancellationToken ct)
    {
        var r = await services.LegendePhotoAsync(id, photoId, legende, ct);
        if (Request.Headers.Accept.ToString().Contains("application/json")) return r.Reussi ? NoContent() : BadRequest(new { erreur = r.Erreur });
        return Retour(r, "Légende enregistrée.", id);
    }

    [HttpPost("{id:int}/photos/{photoId:int}/deplacer")]
    public async Task<IActionResult> DeplacerPhoto(int id, int photoId, int decalage, CancellationToken ct) =>
        Retour(await services.DeplacerPhotoAsync(id, photoId, decalage, ct), null, id);

    [HttpPost("{id:int}/photos/{photoId:int}/supprimer")]
    public async Task<IActionResult> SupprimerPhoto(int id, int photoId, CancellationToken ct) =>
        Retour(await services.SupprimerPhotoAsync(id, photoId, ct), "Photo supprimée.", id);

    private IActionResult Retour(Services.ResultatOperation r, string? succes, int id)
    {
        if (!r.Reussi) Erreur(r.Erreur ?? "Opération impossible.");
        else if (succes is not null) Succes(succes);
        return Redirect($"/admin/services/{id}#photos");
    }
}
