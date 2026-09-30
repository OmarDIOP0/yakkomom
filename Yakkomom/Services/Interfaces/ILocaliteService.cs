using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

public interface ILocaliteService
{
    /// <summary>Toutes les localités (en cache), triées par niveau puis ordre.</summary>
    Task<IReadOnlyList<LocaliteOption>> ListerAsync(CancellationToken ct = default);

    /// <summary>
    /// Vérifie la cohérence Région &gt; Département &gt; Commune et complète les niveaux
    /// supérieurs à partir du plus précis (choisir une commune suffit).
    /// </summary>
    Task<(int? RegionId, int? DepartementId, int? CommuneId, string? Erreur)> ResoudreAsync(
        int? regionId, int? departementId, int? communeId, CancellationToken ct = default);

    Task<ResultatOperation> AjouterAsync(string nom, TypeLocalite type, int? parentId, CancellationToken ct = default);
    Task<ResultatOperation> RenommerAsync(int id, string nom, CancellationToken ct = default);
    Task<ResultatOperation> BasculerActiveAsync(int id, CancellationToken ct = default);
}
