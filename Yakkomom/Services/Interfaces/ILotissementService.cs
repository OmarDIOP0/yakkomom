using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

/// <summary>Découpage d'un grand terrain en lots (admin).</summary>
public interface ILotissementService
{
    Task<LotsAdminVm?> ListerAsync(int terrainId, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerFourchetteAsync(int terrainId, FourchetteVm vm, CancellationToken ct = default);
    Task<ResultatOperation> CreerSerieAsync(int terrainId, SerieLotsVm vm, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerLotAsync(int terrainId, int lotId, LotSaisieVm vm, CancellationToken ct = default);
    Task<ResultatOperation> ChangerStatutAsync(int terrainId, int lotId, StatutLot statut, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerAsync(int terrainId, int lotId, CancellationToken ct = default);
}
