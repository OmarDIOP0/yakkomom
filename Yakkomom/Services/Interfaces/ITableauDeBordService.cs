using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

/// <summary>Statistiques du tableau de bord et consultation du journal des actions.</summary>
public interface ITableauDeBordService
{
    /// <param name="jours">Période des statistiques de clics (7, 30 ou 90 jours).</param>
    Task<TableauDeBordVm> TableauAsync(int jours, CancellationToken ct = default);

    Task<JournalVm> JournalAsync(FiltreJournal filtre, bool voirComptes, CancellationToken ct = default);
}
