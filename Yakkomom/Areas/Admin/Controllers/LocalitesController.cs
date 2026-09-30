using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.Enums;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

[Route("admin/localites")]
public class LocalitesController(ILocaliteService localites) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? ouvert, CancellationToken ct)
    {
        ViewData["Ouvert"] = ouvert;
        return View(await localites.ListerAsync(ct));
    }

    [HttpPost("ajouter")]
    public async Task<IActionResult> Ajouter(string nom, TypeLocalite type, int? parentId, int? ouvert, CancellationToken ct)
    {
        var r = await localites.AjouterAsync(nom ?? "", type, parentId, ct);
        if (r.Reussi) Succes($"« {nom?.Trim()} » ajouté."); else Erreur(r.Erreur!);
        return RedirectToAction(nameof(Index), new { ouvert });
    }

    [HttpPost("{id:int}/renommer")]
    public async Task<IActionResult> Renommer(int id, string nom, int? ouvert, CancellationToken ct)
    {
        var r = await localites.RenommerAsync(id, nom ?? "", ct);
        if (r.Reussi) Succes("Localité renommée."); else Erreur(r.Erreur!);
        return RedirectToAction(nameof(Index), new { ouvert });
    }

    [HttpPost("{id:int}/visibilite")]
    public async Task<IActionResult> Visibilite(int id, int? ouvert, CancellationToken ct)
    {
        var r = await localites.BasculerActiveAsync(id, ct);
        if (r.Reussi) Succes("Visibilité modifiée."); else Erreur(r.Erreur!);
        return RedirectToAction(nameof(Index), new { ouvert });
    }
}
