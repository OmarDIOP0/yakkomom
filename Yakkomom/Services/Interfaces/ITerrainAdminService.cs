using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

/// <summary>Gestion des terrains dans l'admin. Seul le titre est obligatoire.</summary>
public interface ITerrainAdminService
{
    Task<ListeTerrainsAdminVm> ListerAsync(FiltreTerrainsAdmin filtre, CancellationToken ct = default);

    /// <summary>Terrain avec photos, documents et commune (lecture seule).</summary>
    Task<Terrain?> ObtenirAsync(int id, CancellationToken ct = default);
    Task<EnTeteTerrainVm?> EnTeteAsync(int id, OngletTerrain onglet, CancellationToken ct = default);

    Task<Terrain> CreerBrouillonAsync(string titre, CancellationToken ct = default);

    Task<ResultatOperation> EnregistrerInfosAsync(int id, InfosTerrainVm vm, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerLocalisationAsync(int id, LocalisationTerrainVm vm, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerMediasAsync(int id, MediasTerrainVm vm, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerDocumentsAsync(int id, DocumentsTerrainVm vm, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerContactsAsync(int id, ContactsTerrainVm vm, CancellationToken ct = default);

    Task<ResultatOperation> ChangerStatutAsync(int id, StatutTerrain statut, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerAsync(int id, CancellationToken ct = default);
}
