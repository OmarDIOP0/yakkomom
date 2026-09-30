using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Services;

public partial class MediaService(
    YakkomomDbContext db,
    IStorageService stockage,
    IJournalService journal,
    IParametreSiteService parametres,
    IHttpContextAccessor http,
    ILogger<MediaService> logger) : IMediaService
{
    public const int PhotosMax = 40;
    public const int DocumentsMax = 30;
    public const int LargeurVignette = 480;
    private static readonly TimeSpan DureeLienDocument = TimeSpan.FromMinutes(5);

    private string? UtilisateurId => http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string UrlImage(string cle, int? largeur = null) => stockage.UrlImage(cle, largeur);

    // =====================================================================
    // Photos
    // =====================================================================
    public async Task<IReadOnlyList<PhotoAdminVm>> ListerPhotosAsync(int terrainId, CancellationToken ct = default)
    {
        var photos = await db.TerrainPhotos.AsNoTracking()
            .Where(p => p.TerrainId == terrainId).OrderBy(p => p.Ordre).ThenBy(p => p.Id).ToListAsync(ct);
        return photos.Select(VersVm).ToList();
    }

    public async Task<ResultatMedia<PhotoAdminVm>> AjouterPhotoAsync(int terrainId, IFormFile? fichier, IFormFile? vignette,
        EnvoiPhoto infos, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == terrainId, ct);
        if (terrain is null) return ResultatMedia<PhotoAdminVm>.Echec("Terrain introuvable.");

        // Renvoi après une coupure : la photo a peut-être déjà été reçue.
        if (infos.IdEnvoi is { } idEnvoi &&
            await db.TerrainPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.TerrainId == terrainId && p.IdEnvoi == idEnvoi, ct) is { } existante)
            return ResultatMedia<PhotoAdminVm>.Ok(VersVm(existante));

        if (await db.TerrainPhotos.CountAsync(p => p.TerrainId == terrainId, ct) >= PhotosMax)
            return ResultatMedia<PhotoAdminVm>.Echec($"Maximum {PhotosMax} photos par terrain.");

        var (infoPhoto, erreur) = await ValiderImageAsync(fichier, ReglesEnvoi.PhotoTypes, ReglesEnvoi.PhotoTailleMax,
            ReglesEnvoi.PhotoDimensionMin, ReglesEnvoi.PhotoDimensionMax, ct);
        if (erreur is not null) return ResultatMedia<PhotoAdminVm>.Echec(erreur);

        // La vignette n'est utile que si le stockage ne sait pas redimensionner (stockage local).
        InfosFichier? infoVignette = null;
        if (vignette is not null && !stockage.RedimensionneALaVolee)
        {
            (infoVignette, erreur) = await ValiderImageAsync(vignette, ReglesEnvoi.PhotoTypes, ReglesEnvoi.VignetteTailleMax,
                32, ReglesEnvoi.VignetteDimensionMax, ct);
            if (erreur is not null) return ResultatMedia<PhotoAdminVm>.Echec("Vignette : " + erreur);
        }

        var nom = Guid.NewGuid().ToString("N");
        var cle = $"terrains/{terrainId}/photos/{nom}{infoPhoto!.Extension}";
        var cleVignette = infoVignette is null ? null : $"terrains/{terrainId}/photos/{nom}-{LargeurVignette}{infoVignette.Extension}";

        await using (var flux = fichier!.OpenReadStream())
            await stockage.EnregistrerAsync(flux, cle, infoPhoto.TypeMime, ZoneStockage.Publique, ct);
        if (cleVignette is not null)
            await using (var flux = vignette!.OpenReadStream())
                await stockage.EnregistrerAsync(flux, cleVignette, infoVignette!.TypeMime, ZoneStockage.Publique, ct);

        var ordre = await db.TerrainPhotos.Where(p => p.TerrainId == terrainId).MaxAsync(p => (int?)p.Ordre, ct) ?? -1;
        var photo = new TerrainPhoto
        {
            TerrainId = terrainId,
            CleStockage = cle,
            CleVignette = cleVignette,
            TypeMime = infoPhoto.TypeMime,
            IdEnvoi = infos.IdEnvoi,
            Largeur = infoPhoto.Largeur!.Value,
            Hauteur = infoPhoto.Hauteur!.Value,
            TailleOctets = fichier.Length,
            CouleurDominante = infos.CouleurDominante is { } c && RegexCouleur().IsMatch(c) ? c.ToUpperInvariant() : null,
            Legende = Nettoyer(infos.Legende, 200),
            Ordre = ordre + 1,
            EstCouverture = !await db.TerrainPhotos.AnyAsync(p => p.TerrainId == terrainId && p.EstCouverture, ct)
        };
        db.TerrainPhotos.Add(photo);
        Toucher(terrain);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Échec en base (ou doublon simultané) : on ne laisse pas de fichier orphelin.
            logger.LogWarning(ex, "Enregistrement de la photo impossible, nettoyage des fichiers.");
            await SupprimerSansErreurAsync(cle, ZoneStockage.Publique);
            if (cleVignette is not null) await SupprimerSansErreurAsync(cleVignette, ZoneStockage.Publique);
            if (infos.IdEnvoi is { } id2 &&
                await db.TerrainPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.TerrainId == terrainId && p.IdEnvoi == id2, ct) is { } doublon)
                return ResultatMedia<PhotoAdminVm>.Ok(VersVm(doublon));
            return ResultatMedia<PhotoAdminVm>.Echec("Enregistrement impossible, réessayez.");
        }

        await journal.EnregistrerAsync(TypeAction.Creation, nameof(TerrainPhoto), photo.Id.ToString(),
            $"{terrain.Reference} : ajout d'une photo ({ReglesEnvoi.TailleLisible(photo.TailleOctets)})", ct: ct);
        return ResultatMedia<PhotoAdminVm>.Ok(VersVm(photo));
    }

    public async Task<ResultatOperation> DefinirCouvertureAsync(int terrainId, int photoId, CancellationToken ct = default)
    {
        var photos = await db.TerrainPhotos.Where(p => p.TerrainId == terrainId).ToListAsync(ct);
        var cible = photos.FirstOrDefault(p => p.Id == photoId);
        if (cible is null) return ResultatOperation.Echec("Photo introuvable.");
        if (cible.EstCouverture) return ResultatOperation.Ok();

        // Deux étapes dans une transaction : l'index unique interdit deux couvertures simultanées.
        // La stratégie de nouvelle tentative (coupures réseau) impose de passer par ExecuteAsync.
        var strategie = db.Database.CreateExecutionStrategy();
        await strategie.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            foreach (var p in photos.Where(p => p.EstCouverture)) p.EstCouverture = false;
            await db.SaveChangesAsync(ct);
            cible.EstCouverture = true;
            await ToucherAsync(terrainId, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });

        await journal.EnregistrerAsync(TypeAction.Modification, nameof(TerrainPhoto), photoId.ToString(), "Nouvelle photo de couverture", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> ModifierLegendeAsync(int terrainId, int photoId, string? legende, CancellationToken ct = default)
    {
        var photo = await db.TerrainPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.TerrainId == terrainId, ct);
        if (photo is null) return ResultatOperation.Echec("Photo introuvable.");
        photo.Legende = Nettoyer(legende, 200);
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> OrdonnerPhotosAsync(int terrainId, IReadOnlyList<int> ordre, CancellationToken ct = default)
    {
        var photos = await db.TerrainPhotos.Where(p => p.TerrainId == terrainId).ToListAsync(ct);
        if (ordre.Count != photos.Count || !photos.Select(p => p.Id).ToHashSet().SetEquals(ordre))
            return ResultatOperation.Echec("La liste des photos a changé entre-temps. Rechargez la page.");

        var index = photos.ToDictionary(p => p.Id);
        for (var i = 0; i < ordre.Count; i++) index[ordre[i]].Ordre = i;
        await ToucherAsync(terrainId, ct);
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> DeplacerPhotoAsync(int terrainId, int photoId, int decalage, CancellationToken ct = default)
    {
        var ids = await db.TerrainPhotos.Where(p => p.TerrainId == terrainId)
            .OrderBy(p => p.Ordre).ThenBy(p => p.Id).Select(p => p.Id).ToListAsync(ct);
        var i = ids.IndexOf(photoId);
        if (i < 0) return ResultatOperation.Echec("Photo introuvable.");
        var j = Math.Clamp(i + Math.Sign(decalage), 0, ids.Count - 1);
        (ids[i], ids[j]) = (ids[j], ids[i]);
        return await OrdonnerPhotosAsync(terrainId, ids, ct);
    }

    public async Task<ResultatOperation> SupprimerPhotoAsync(int terrainId, int photoId, CancellationToken ct = default)
    {
        var photo = await db.TerrainPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.TerrainId == terrainId, ct);
        if (photo is null) return ResultatOperation.Echec("Photo introuvable.");

        db.TerrainPhotos.Remove(photo);
        await ToucherAsync(terrainId, ct);
        await db.SaveChangesAsync(ct);

        // La couverture passe à la première photo restante.
        if (photo.EstCouverture &&
            await db.TerrainPhotos.Where(p => p.TerrainId == terrainId).OrderBy(p => p.Ordre).FirstOrDefaultAsync(ct) is { } suivante)
        {
            suivante.EstCouverture = true;
            await db.SaveChangesAsync(ct);
        }

        await SupprimerSansErreurAsync(photo.CleStockage, ZoneStockage.Publique);
        if (photo.CleVignette is not null) await SupprimerSansErreurAsync(photo.CleVignette, ZoneStockage.Publique);
        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(TerrainPhoto), photoId.ToString(), "Suppression d'une photo", ct: ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    // Documents fonciers
    // =====================================================================
    public async Task<IReadOnlyList<DocumentAdminVm>> ListerDocumentsAsync(int terrainId, CancellationToken ct = default) =>
        await db.DocumentsFonciers.AsNoTracking()
            .Where(d => d.TerrainId == terrainId).OrderBy(d => d.Type).ThenByDescending(d => d.CreeLe)
            .Select(d => new DocumentAdminVm
            {
                Id = d.Id, Type = d.Type, Titre = d.Titre, NomFichier = d.NomFichierOriginal, TypeMime = d.TypeMime,
                TailleOctets = d.TailleOctets, EstPublic = d.EstPublic, CreeLe = d.CreeLe
            })
            .ToListAsync(ct);

    public async Task<ResultatMedia<DocumentAdminVm>> AjouterDocumentAsync(int terrainId, IFormFile? fichier, EnvoiDocument infos,
        CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == terrainId, ct);
        if (terrain is null) return ResultatMedia<DocumentAdminVm>.Echec("Terrain introuvable.");
        if (!Enum.IsDefined(infos.Type)) return ResultatMedia<DocumentAdminVm>.Echec("Type de document inconnu.");

        if (infos.IdEnvoi is { } idEnvoi &&
            await db.DocumentsFonciers.AsNoTracking().FirstOrDefaultAsync(d => d.TerrainId == terrainId && d.IdEnvoi == idEnvoi, ct) is { } existant)
            return ResultatMedia<DocumentAdminVm>.Ok(VersVm(existant));

        if (await db.DocumentsFonciers.CountAsync(d => d.TerrainId == terrainId, ct) >= DocumentsMax)
            return ResultatMedia<DocumentAdminVm>.Echec($"Maximum {DocumentsMax} documents par terrain.");

        if (fichier is null || fichier.Length == 0) return ResultatMedia<DocumentAdminVm>.Echec("Aucun fichier reçu.");
        if (fichier.Length > ReglesEnvoi.DocumentTailleMax)
            return ResultatMedia<DocumentAdminVm>.Echec($"Fichier trop lourd (maximum {ReglesEnvoi.TailleLisible(ReglesEnvoi.DocumentTailleMax)}).");

        InfosFichier? info;
        await using (var flux = fichier.OpenReadStream()) info = await AnalyseFichier.AnalyserAsync(flux, ct);
        if (info is null || !ReglesEnvoi.DocumentTypes.Contains(info.TypeMime))
            return ResultatMedia<DocumentAdminVm>.Echec("Format non accepté : PDF, JPEG, PNG ou WebP uniquement.");

        var cle = $"terrains/{terrainId}/documents/{Guid.NewGuid():N}{info.Extension}";
        await using (var flux = fichier.OpenReadStream())
            await stockage.EnregistrerAsync(flux, cle, info.TypeMime, ZoneStockage.Privee, ct);

        var document = new DocumentFoncier
        {
            TerrainId = terrainId,
            Type = infos.Type,
            Titre = Nettoyer(infos.Titre, 200),
            CleStockage = cle,
            NomFichierOriginal = NomFichierSur(fichier.FileName, info.Extension),
            TypeMime = info.TypeMime,
            TailleOctets = fichier.Length,
            EstPublic = infos.EstPublic,
            IdEnvoi = infos.IdEnvoi,
            CreeParId = UtilisateurId
        };
        db.DocumentsFonciers.Add(document);
        // Un document déposé renseigne la situation foncière si elle ne l'était pas.
        terrain.SituationFonciere ??= infos.Type;
        Toucher(terrain);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Enregistrement du document impossible, nettoyage du fichier.");
            await SupprimerSansErreurAsync(cle, ZoneStockage.Privee);
            return ResultatMedia<DocumentAdminVm>.Echec("Enregistrement impossible, réessayez.");
        }

        await journal.EnregistrerAsync(TypeAction.Creation, nameof(DocumentFoncier), document.Id.ToString(),
            $"{terrain.Reference} : ajout du document « {document.NomFichierOriginal} » ({(document.EstPublic ? "public" : "privé")})", ct: ct);
        return ResultatMedia<DocumentAdminVm>.Ok(VersVm(document));
    }

    public async Task<ResultatOperation> BasculerVisibiliteDocumentAsync(int terrainId, int documentId, CancellationToken ct = default)
    {
        var document = await db.DocumentsFonciers.Include(d => d.Terrain)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TerrainId == terrainId, ct);
        if (document is null) return ResultatOperation.Echec("Document introuvable.");

        document.EstPublic = !document.EstPublic;
        Toucher(document.Terrain);
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(DocumentFoncier), documentId.ToString(),
            $"{document.Terrain.Reference} : document « {document.NomFichierOriginal} » rendu {(document.EstPublic ? "PUBLIC" : "privé")}", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> SupprimerDocumentAsync(int terrainId, int documentId, CancellationToken ct = default)
    {
        var document = await db.DocumentsFonciers.FirstOrDefaultAsync(d => d.Id == documentId && d.TerrainId == terrainId, ct);
        if (document is null) return ResultatOperation.Echec("Document introuvable.");

        db.DocumentsFonciers.Remove(document);
        await ToucherAsync(terrainId, ct);
        await db.SaveChangesAsync(ct);
        await SupprimerSansErreurAsync(document.CleStockage, ZoneStockage.Privee);
        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(DocumentFoncier), documentId.ToString(),
            $"Suppression du document « {document.NomFichierOriginal} »", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<(DocumentFoncier Document, FichierPrive Fichier)?> OuvrirDocumentAsync(int documentId, bool accesAdmin, CancellationToken ct = default)
    {
        var document = await db.DocumentsFonciers.AsNoTracking().Include(d => d.Terrain)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null) return null;
        if (!accesAdmin && !(document.EstPublic && document.Terrain.EstPublic)) return null;

        var fichier = await stockage.OuvrirPriveAsync(document.CleStockage, document.TypeMime, DureeLienDocument, ct);
        return fichier is null ? null : (document, fichier);
    }

    // =====================================================================
    // Logo
    // =====================================================================
    public async Task<ResultatOperation> DefinirLogoAsync(IFormFile? fichier, CancellationToken ct = default)
    {
        var (info, erreur) = await ValiderImageAsync(fichier, ReglesEnvoi.LogoTypes, ReglesEnvoi.LogoTailleMax, 32, ReglesEnvoi.LogoDimensionMax, ct);
        if (erreur is not null) return ResultatOperation.Echec(erreur);

        var cle = $"site/logo-{Guid.NewGuid():N}{info!.Extension}";
        await using (var flux = fichier!.OpenReadStream())
            await stockage.EnregistrerAsync(flux, cle, info.TypeMime, ZoneStockage.Publique, ct);

        var p = await db.ParametresSite.FirstAsync(x => x.Id == ParametreSite.IdUnique, ct);
        var ancien = p.CleLogo;
        p.CleLogo = cle;
        p.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        parametres.InvaliderCache();
        if (ancien is not null) await SupprimerSansErreurAsync(ancien, ZoneStockage.Publique);

        await journal.EnregistrerAsync(TypeAction.Modification, nameof(ParametreSite), p.Id.ToString(), "Nouveau logo", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> SupprimerLogoAsync(CancellationToken ct = default)
    {
        var p = await db.ParametresSite.FirstAsync(x => x.Id == ParametreSite.IdUnique, ct);
        if (p.CleLogo is null) return ResultatOperation.Ok();
        var ancien = p.CleLogo;
        p.CleLogo = null;
        await db.SaveChangesAsync(ct);
        parametres.InvaliderCache();
        await SupprimerSansErreurAsync(ancien, ZoneStockage.Publique);
        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(ParametreSite), p.Id.ToString(), "Suppression du logo", ct: ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    // Nettoyage
    // =====================================================================
    public async Task SupprimerFichiersTerrainAsync(int terrainId, CancellationToken ct = default)
    {
        var photos = await db.TerrainPhotos.AsNoTracking().Where(p => p.TerrainId == terrainId)
            .Select(p => new { p.CleStockage, p.CleVignette }).ToListAsync(ct);
        var documents = await db.DocumentsFonciers.AsNoTracking().Where(d => d.TerrainId == terrainId)
            .Select(d => d.CleStockage).ToListAsync(ct);

        foreach (var p in photos)
        {
            await SupprimerSansErreurAsync(p.CleStockage, ZoneStockage.Publique);
            if (p.CleVignette is not null) await SupprimerSansErreurAsync(p.CleVignette, ZoneStockage.Publique);
        }
        foreach (var cle in documents) await SupprimerSansErreurAsync(cle, ZoneStockage.Privee);

        var panoramas = await db.Panoramas.AsNoTracking().Where(p => p.TerrainId == terrainId)
            .Select(p => new[] { p.CleStockageHd, p.CleStockageBd, p.CleVignette }).ToListAsync(ct);
        foreach (var cle in panoramas.SelectMany(c => c).OfType<string>()) await SupprimerSansErreurAsync(cle, ZoneStockage.Publique);
    }

    // =====================================================================
    // Outils
    // =====================================================================
    internal static async Task<(InfosFichier?, string?)> ValiderImageAsync(IFormFile? fichier, string[] types, long tailleMax,
        int dimensionMin, int dimensionMax, CancellationToken ct)
    {
        if (fichier is null || fichier.Length == 0) return (null, "Aucun fichier reçu.");
        if (fichier.Length > tailleMax) return (null, $"Fichier trop lourd (maximum {ReglesEnvoi.TailleLisible(tailleMax)}).");

        InfosFichier? info;
        await using (var flux = fichier.OpenReadStream()) info = await AnalyseFichier.AnalyserAsync(flux, ct);
        if (info is null || !types.Contains(info.TypeMime)) return (null, "Format d'image non accepté.");
        if (info.Largeur is null || info.Hauteur is null) return (null, "Dimensions de l'image illisibles.");
        var cote = Math.Max(info.Largeur.Value, info.Hauteur.Value);
        if (cote > dimensionMax) return (null, $"Image trop grande ({info.Largeur}×{info.Hauteur} px, maximum {dimensionMax} px).");
        if (Math.Min(info.Largeur.Value, info.Hauteur.Value) < dimensionMin) return (null, "Image trop petite.");
        return (info, null);
    }

    private PhotoAdminVm VersVm(TerrainPhoto p) => new()
    {
        Id = p.Id,
        UrlVignette = p.CleVignette is not null ? stockage.UrlImage(p.CleVignette) : stockage.UrlImage(p.CleStockage, LargeurVignette),
        UrlGrande = stockage.UrlImage(p.CleStockage),
        Largeur = p.Largeur, Hauteur = p.Hauteur, TailleOctets = p.TailleOctets,
        CouleurDominante = p.CouleurDominante, Legende = p.Legende, Ordre = p.Ordre, EstCouverture = p.EstCouverture
    };

    private static DocumentAdminVm VersVm(DocumentFoncier d) => new()
    {
        Id = d.Id, Type = d.Type, Titre = d.Titre, NomFichier = d.NomFichierOriginal, TypeMime = d.TypeMime,
        TailleOctets = d.TailleOctets, EstPublic = d.EstPublic, CreeLe = d.CreeLe
    };

    private async Task SupprimerSansErreurAsync(string cle, ZoneStockage zone)
    {
        try { await stockage.SupprimerAsync(cle, zone); }
        catch (Exception ex) { logger.LogWarning(ex, "Fichier {Cle} non supprimé du stockage.", cle); }
    }

    private void Toucher(Terrain t)
    {
        t.ModifieLe = DateTime.UtcNow;
        t.ModifieParId = UtilisateurId;
    }

    private async Task ToucherAsync(int terrainId, CancellationToken ct)
    {
        var t = await db.Terrains.FirstOrDefaultAsync(x => x.Id == terrainId, ct);
        if (t is not null) Toucher(t);
    }

    /// <summary>Nom de fichier affichable (le fichier est toujours stocké sous un nom aléatoire).</summary>
    private static string NomFichierSur(string? nom, string extension)
    {
        var base_ = Path.GetFileNameWithoutExtension(nom ?? "");
        base_ = new string(base_.Where(c => !char.IsControl(c) && c is not ('"' or '\\' or '/' or ':' or '*' or '?' or '<' or '>' or '|')).ToArray()).Trim();
        if (base_.Length == 0) base_ = "document";
        if (base_.Length > 120) base_ = base_[..120];
        return base_ + extension;
    }

    private static string? Nettoyer(string? s, int max) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > max ? s.Trim()[..max] : s.Trim());

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex RegexCouleur();
}
