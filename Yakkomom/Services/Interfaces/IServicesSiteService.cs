using Yakkomom.Models.ViewModels;
using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

/// <summary>Les services Yakkomom (vente de terrains, construction…) : pages publiques et gestion admin.</summary>
public interface IServicesSiteService
{
    // Public
    Task<IReadOnlyList<ServiceCarteVm>> ListerPublicAsync(CancellationToken ct = default);
    Task<ServicePublicVm?> DetailPublicAsync(string slug, CancellationToken ct = default);

    // Admin
    Task<IReadOnlyList<ServiceLigneVm>> ListerAdminAsync(CancellationToken ct = default);
    Task<ServiceEditionVm?> EditionAsync(int id, CancellationToken ct = default);
    Task<int> CreerAsync(string titre, CancellationToken ct = default);
    Task<ResultatOperation> EnregistrerAsync(ServiceEditionVm vm, CancellationToken ct = default);
    Task<ResultatOperation> DeplacerAsync(int id, int decalage, CancellationToken ct = default);

    // Photos de réalisations (la première sert de couverture)
    Task<ResultatMedia<PhotoAdminVm>> AjouterPhotoAsync(int serviceId, IFormFile? fichier, IFormFile? vignette, EnvoiPhoto infos, CancellationToken ct = default);
    Task<ResultatOperation> LegendePhotoAsync(int serviceId, int photoId, string? legende, CancellationToken ct = default);
    Task<ResultatOperation> OrdonnerPhotosAsync(int serviceId, IReadOnlyList<int> ordre, CancellationToken ct = default);
    Task<ResultatOperation> DeplacerPhotoAsync(int serviceId, int photoId, int decalage, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerPhotoAsync(int serviceId, int photoId, CancellationToken ct = default);
}
