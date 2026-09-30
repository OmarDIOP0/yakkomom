using Yakkomom.Models.ViewModels;

namespace Yakkomom.Services.Interfaces;

/// <summary>Visite 360° : panoramas équirectangulaires reliés par des points de passage (Pannellum).</summary>
public interface IVisite360Service
{
    Task<IReadOnlyList<PanoramaAdminVm>> ListerAsync(int terrainId, CancellationToken ct = default);
    Task<ResultatMedia<PanoramaAdminVm>> AjouterAsync(int terrainId, IFormFile? hd, IFormFile? bd, IFormFile? vignette, EnvoiPanorama infos, CancellationToken ct = default);
    Task<ResultatOperation> RenommerAsync(int terrainId, int panoramaId, string? titre, CancellationToken ct = default);
    Task<ResultatOperation> DefinirDepartAsync(int terrainId, int panoramaId, CancellationToken ct = default);
    Task<ResultatOperation> DeplacerAsync(int terrainId, int panoramaId, int decalage, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerAsync(int terrainId, int panoramaId, CancellationToken ct = default);

    Task<ResultatOperation> EnregistrerVueAsync(int terrainId, int panoramaId, VueInitiale vue, CancellationToken ct = default);
    Task<ResultatMedia<int>> AjouterHotspotAsync(int terrainId, int panoramaId, NouveauHotspot hotspot, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerHotspotAsync(int terrainId, int hotspotId, CancellationToken ct = default);

    /// <summary>Configuration Pannellum (JSON) ; <paramref name="hd"/> : images haute définition.</summary>
    Task<string?> ConfigurationAsync(int terrainId, bool hd, bool edition, CancellationToken ct = default);

    Task<VisitePubliqueVm?> VisitePubliqueAsync(int terrainId, CancellationToken ct = default);
}
