using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Onglet « Lots » : découpage d'un grand terrain (lotissement).</summary>
public partial class TerrainsController
{
    [HttpGet("{id:int}/lots")]
    public async Task<IActionResult> Lots(int id, [FromServices] ILotissementService lots, CancellationToken ct)
    {
        var vm = await lots.ListerAsync(id, ct);
        if (vm is null) return NotFound();
        vm.EnTete = await terrains.EnTeteAsync(id, OngletTerrain.Lots, ct);
        return View("Lots", vm);
    }

    [HttpPost("{id:int}/lots/fourchette")]
    public async Task<IActionResult> FourchetteLots(int id, [Bind(Prefix = "Fourchette")] FourchetteVm vm,
        [FromServices] ILotissementService lots, CancellationToken ct) =>
        Retour(id, await lots.EnregistrerFourchetteAsync(id, vm, ct), "Fourchette de prix enregistrée.");

    [HttpPost("{id:int}/lots/serie")]
    public async Task<IActionResult> SerieLots(int id, [Bind(Prefix = "Serie")] SerieLotsVm vm,
        [FromServices] ILotissementService lots, CancellationToken ct) =>
        Retour(id, await lots.CreerSerieAsync(id, vm, ct), null);

    [HttpPost("{id:int}/lots/{lotId:int}")]
    public async Task<IActionResult> EnregistrerLot(int id, int lotId, LotSaisieVm vm,
        [FromServices] ILotissementService lots, CancellationToken ct) =>
        Retour(id, await lots.EnregistrerLotAsync(id, lotId, vm, ct), $"Lot {vm.Numero} enregistré.", lotId);

    [HttpPost("{id:int}/lots/{lotId:int}/statut")]
    public async Task<IActionResult> StatutLot(int id, int lotId, StatutLot statut,
        [FromServices] ILotissementService lots, CancellationToken ct) =>
        Retour(id, await lots.ChangerStatutAsync(id, lotId, statut, ct), null, lotId);

    [HttpPost("{id:int}/lots/{lotId:int}/supprimer")]
    public async Task<IActionResult> SupprimerLot(int id, int lotId, [FromServices] ILotissementService lots, CancellationToken ct) =>
        Retour(id, await lots.SupprimerAsync(id, lotId, ct), "Lot supprimé.");

    private RedirectResult Retour(int id, ResultatOperation r, string? succes, int? lotId = null)
    {
        if (r.Reussi) { if ((r.Message ?? succes) is { } m) Succes(m); }
        else Erreur(r.Erreur ?? string.Join(" ", r.Erreurs.Values));
        return Redirect($"/admin/terrains/{id}/lots" + (lotId is null ? "" : $"#lot-{lotId}"));
    }
}
