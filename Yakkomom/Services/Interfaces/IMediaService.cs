using Yakkomom.Models.Entities;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Stockage;

namespace Yakkomom.Services.Interfaces;

public record ResultatMedia<T>(T? Valeur, string? Erreur)
{
    public bool Reussi => Erreur is null;
    public static ResultatMedia<T> Ok(T valeur) => new(valeur, null);
    public static ResultatMedia<T> Echec(string erreur) => new(default, erreur);
}

/// <summary>Photos, documents fonciers et logo : validation, stockage et métadonnées.</summary>
public interface IMediaService
{
    // --- Photos -----------------------------------------------------------
    Task<IReadOnlyList<PhotoAdminVm>> ListerPhotosAsync(int terrainId, CancellationToken ct = default);
    Task<ResultatMedia<PhotoAdminVm>> AjouterPhotoAsync(int terrainId, IFormFile? fichier, IFormFile? vignette, EnvoiPhoto infos, CancellationToken ct = default);
    Task<ResultatOperation> DefinirCouvertureAsync(int terrainId, int photoId, CancellationToken ct = default);
    Task<ResultatOperation> ModifierLegendeAsync(int terrainId, int photoId, string? legende, CancellationToken ct = default);
    Task<ResultatOperation> OrdonnerPhotosAsync(int terrainId, IReadOnlyList<int> ordre, CancellationToken ct = default);
    Task<ResultatOperation> DeplacerPhotoAsync(int terrainId, int photoId, int decalage, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerPhotoAsync(int terrainId, int photoId, CancellationToken ct = default);

    // --- Documents fonciers --------------------------------------------
    Task<IReadOnlyList<DocumentAdminVm>> ListerDocumentsAsync(int terrainId, CancellationToken ct = default);
    Task<ResultatMedia<DocumentAdminVm>> AjouterDocumentAsync(int terrainId, IFormFile? fichier, EnvoiDocument infos, CancellationToken ct = default);
    Task<ResultatOperation> BasculerVisibiliteDocumentAsync(int terrainId, int documentId, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerDocumentAsync(int terrainId, int documentId, CancellationToken ct = default);

    /// <summary>
    /// Ouvre un document. Si <paramref name="accesAdmin"/> est faux, le document doit être public
    /// ET son terrain visible sur le site : sinon null (le contrôleur répond 404, sans rien révéler).
    /// </summary>
    Task<(DocumentFoncier Document, FichierPrive Fichier)?> OuvrirDocumentAsync(int documentId, bool accesAdmin, CancellationToken ct = default);

    // --- Logo -------------------------------------------------------------
    Task<ResultatOperation> DefinirLogoAsync(IFormFile? fichier, CancellationToken ct = default);
    Task<ResultatOperation> SupprimerLogoAsync(CancellationToken ct = default);

    // --- Nettoyage --------------------------------------------------------
    /// <summary>Supprime du stockage tous les fichiers d'un terrain (avant sa suppression en base).</summary>
    Task SupprimerFichiersTerrainAsync(int terrainId, CancellationToken ct = default);

    /// <summary>URL publique d'une image stockée.</summary>
    string UrlImage(string cle, int? largeur = null);
}
