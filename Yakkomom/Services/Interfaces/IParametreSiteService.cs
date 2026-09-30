using Yakkomom.Models.Entities;
using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

/// <summary>Paramètres globaux du site, mis en cache (lus sur chaque page).</summary>
public interface IParametreSiteService
{
    Task<ParametreSite> ObtenirAsync(CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerAsync(ParametresVm vm, CancellationToken ct = default);
    /// <summary>À appeler après une modification dans l'admin.</summary>
    void InvaliderCache();
}
