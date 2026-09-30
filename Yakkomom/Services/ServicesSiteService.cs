using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Services;

public class ServicesSiteService(
    YakkomomDbContext db,
    IStorageService stockage,
    ImagesPubliques images,
    IParametreSiteService parametres,
    IJournalService journal,
    Seo seo,
    ILogger<ServicesSiteService> logger) : IServicesSiteService
{
    public const int PhotosMax = 30;

    // =====================================================================
    // Public
    // =====================================================================
    public async Task<IReadOnlyList<ServiceCarteVm>> ListerPublicAsync(CancellationToken ct = default)
    {
        var services = await db.Services.AsNoTracking().Where(s => s.EstActif).OrderBy(s => s.Ordre)
            .Select(s => new { s.Titre, s.Slug, s.Resume, s.Icone, Couverture = s.Photos.OrderBy(p => p.Ordre).FirstOrDefault(), N = s.Photos.Count })
            .ToListAsync(ct);
        return services.Select(s => new ServiceCarteVm(s.Titre, s.Slug, s.Resume, s.Icone,
            s.Couverture is null ? null : images.Photo(s.Couverture), s.N)).ToList();
    }

    public async Task<ServicePublicVm?> DetailPublicAsync(string slug, CancellationToken ct = default)
    {
        var s = await db.Services.AsNoTracking().Include(x => x.Photos.OrderBy(p => p.Ordre))
            .FirstOrDefaultAsync(x => x.Slug == slug && x.EstActif, ct);
        if (s is null) return null;

        var p = await parametres.ObtenirAsync(ct);
        var numeros = WhatsAppLiens.NumerosEffectifs(null, p.ContactsParDefaut);
        return new ServicePublicVm
        {
            Titre = s.Titre, Slug = s.Slug, Resume = s.Resume, Description = s.Description, Icone = s.Icone,
            Photos = s.Photos.Select(images.Photo).ToList(),
            WhatsApp = numeros.Select(n => new ContactWhatsAppVm(n.Index, TelephoneSenegal.Afficher(n.Numero), n.Libelle, $"/wa/service/{s.Slug}/{n.Index}")).ToList(),
            Email = p.ContactsParDefaut.Email,
            Autres = (await ListerPublicAsync(ct)).Where(x => x.Slug != s.Slug).ToList(),
            ImagePartage = s.Photos.FirstOrDefault() is { } couverture ? seo.ImagePhoto(couverture, s.Titre) : seo.ImageParDefaut(p.NomSite)
        };
    }

    // =====================================================================
    // Admin : textes, ordre, visibilité
    // =====================================================================
    public async Task<IReadOnlyList<ServiceLigneVm>> ListerAdminAsync(CancellationToken ct = default)
    {
        var services = await db.Services.AsNoTracking().OrderBy(s => s.Ordre)
            .Select(s => new { s.Id, s.Titre, s.Slug, s.Resume, s.Icone, s.EstActif, N = s.Photos.Count, Couverture = s.Photos.OrderBy(p => p.Ordre).FirstOrDefault() })
            .ToListAsync(ct);
        return services.Select(s => new ServiceLigneVm
        {
            Id = s.Id, Titre = s.Titre, Slug = s.Slug, Resume = s.Resume, Icone = s.Icone, EstActif = s.EstActif, NombrePhotos = s.N,
            UrlCouverture = s.Couverture is null ? null : stockage.UrlImage(s.Couverture.CleVignette ?? s.Couverture.CleStockage, 480)
        }).ToList();
    }

    public async Task<ServiceEditionVm?> EditionAsync(int id, CancellationToken ct = default)
    {
        var s = await db.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return null;
        return new ServiceEditionVm
        {
            Id = s.Id, Slug = s.Slug, Titre = s.Titre, Resume = s.Resume, Description = s.Description, Icone = s.Icone,
            MessageWhatsApp = s.MessageWhatsApp, EstActif = s.EstActif, Photos = await PhotosAsync(id, ct)
        };
    }

    public async Task<int> CreerAsync(string titre, CancellationToken ct = default)
    {
        titre = titre.Trim();
        var slug = SlugHelper.Slugifier(titre);
        var base_ = slug;
        for (var i = 2; await db.Services.AnyAsync(s => s.Slug == slug, ct); i++) slug = $"{base_}-{i}";

        var ordre = await db.Services.MaxAsync(s => (int?)s.Ordre, ct) ?? 0;
        var service = new Service
        {
            Titre = titre, Slug = slug, Ordre = ordre + 1, EstActif = false, Icone = "terrain",
            MessageWhatsApp = $"Bonjour Yakkomom, je souhaite en savoir plus sur votre service « {titre} »."
        };
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Creation, nameof(Service), service.Id.ToString(), $"Création du service « {titre} » (masqué)", ct: ct);
        return service.Id;
    }

    public async Task<ResultatOperation> EnregistrerAsync(ServiceEditionVm vm, CancellationToken ct = default)
    {
        var s = await db.Services.FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
        if (s is null) return ResultatOperation.Echec("Service introuvable.");
        if (vm.Icone is not null && ServiceEditionVm.Icones.All(i => i.Nom != vm.Icone))
            return new ResultatOperation().AjouterErreur(nameof(vm.Icone), "Icône inconnue.");

        // Le slug (adresse de la page) ne change pas quand on renomme : les liens partagés restent valides.
        s.Titre = vm.Titre.Trim();
        s.Resume = Nettoyer(vm.Resume);
        s.Description = Nettoyer(vm.Description);
        s.Icone = vm.Icone;
        s.MessageWhatsApp = Nettoyer(vm.MessageWhatsApp);
        s.EstActif = vm.EstActif;
        s.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(Service), s.Id.ToString(), $"Modification du service « {s.Titre} »", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> DeplacerAsync(int id, int decalage, CancellationToken ct = default)
    {
        var liste = await db.Services.OrderBy(s => s.Ordre).ThenBy(s => s.Id).ToListAsync(ct);
        var i = liste.FindIndex(s => s.Id == id);
        if (i < 0) return ResultatOperation.Echec("Service introuvable.");
        var j = Math.Clamp(i + Math.Sign(decalage), 0, liste.Count - 1);
        (liste[i], liste[j]) = (liste[j], liste[i]);
        for (var k = 0; k < liste.Count; k++) liste[k].Ordre = k + 1;
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    // Photos de réalisations
    // =====================================================================
    public async Task<ResultatMedia<PhotoAdminVm>> AjouterPhotoAsync(int serviceId, IFormFile? fichier, IFormFile? vignette, EnvoiPhoto infos, CancellationToken ct = default)
    {
        if (!await db.Services.AnyAsync(s => s.Id == serviceId, ct)) return ResultatMedia<PhotoAdminVm>.Echec("Service introuvable.");
        if (infos.IdEnvoi is { } idEnvoi &&
            await db.ServicePhotos.AsNoTracking().FirstOrDefaultAsync(p => p.ServiceId == serviceId && p.IdEnvoi == idEnvoi, ct) is { } existante)
            return ResultatMedia<PhotoAdminVm>.Ok(VersVm(existante, false));
        if (await db.ServicePhotos.CountAsync(p => p.ServiceId == serviceId, ct) >= PhotosMax)
            return ResultatMedia<PhotoAdminVm>.Echec($"Maximum {PhotosMax} photos par service.");

        var (info, erreur) = await MediaService.ValiderImageAsync(fichier, ReglesEnvoi.PhotoTypes, ReglesEnvoi.PhotoTailleMax,
            ReglesEnvoi.PhotoDimensionMin, ReglesEnvoi.PhotoDimensionMax, ct);
        if (erreur is not null) return ResultatMedia<PhotoAdminVm>.Echec(erreur);
        InfosFichier? infoV = null;
        if (vignette is not null && !stockage.RedimensionneALaVolee)
        {
            (infoV, erreur) = await MediaService.ValiderImageAsync(vignette, ReglesEnvoi.PhotoTypes, ReglesEnvoi.VignetteTailleMax, 32, ReglesEnvoi.VignetteDimensionMax, ct);
            if (erreur is not null) return ResultatMedia<PhotoAdminVm>.Echec("Vignette : " + erreur);
        }

        var nom = Guid.NewGuid().ToString("N");
        var cle = $"services/{serviceId}/{nom}{info!.Extension}";
        var cleV = infoV is null ? null : $"services/{serviceId}/{nom}-480{infoV.Extension}";
        await using (var f = fichier!.OpenReadStream()) await stockage.EnregistrerAsync(f, cle, info.TypeMime, ZoneStockage.Publique, ct);
        if (cleV is not null) await using (var f = vignette!.OpenReadStream()) await stockage.EnregistrerAsync(f, cleV, infoV!.TypeMime, ZoneStockage.Publique, ct);

        var ordre = await db.ServicePhotos.Where(p => p.ServiceId == serviceId).MaxAsync(p => (int?)p.Ordre, ct) ?? -1;
        var photo = new ServicePhoto
        {
            ServiceId = serviceId, CleStockage = cle, CleVignette = cleV, TypeMime = info.TypeMime, TailleOctets = fichier.Length,
            IdEnvoi = infos.IdEnvoi, Largeur = info.Largeur!.Value, Hauteur = info.Hauteur!.Value,
            CouleurDominante = infos.CouleurDominante is { Length: 7 } c && c[0] == '#' && c[1..].All(Uri.IsHexDigit) ? c.ToUpperInvariant() : null,
            Legende = Nettoyer(infos.Legende, 200), Ordre = ordre + 1
        };
        db.ServicePhotos.Add(photo);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Photo de service non enregistrée, nettoyage.");
            await Supprimer(cle);
            if (cleV is not null) await Supprimer(cleV);
            return ResultatMedia<PhotoAdminVm>.Echec("Enregistrement impossible, réessayez.");
        }
        await journal.EnregistrerAsync(TypeAction.Creation, nameof(ServicePhoto), photo.Id.ToString(),
            $"Service n° {serviceId} : ajout d'une photo ({ReglesEnvoi.TailleLisible(photo.TailleOctets)})", ct: ct);
        return ResultatMedia<PhotoAdminVm>.Ok(VersVm(photo, ordre < 0));
    }

    public async Task<ResultatOperation> LegendePhotoAsync(int serviceId, int photoId, string? legende, CancellationToken ct = default)
    {
        var p = await db.ServicePhotos.FirstOrDefaultAsync(x => x.Id == photoId && x.ServiceId == serviceId, ct);
        if (p is null) return ResultatOperation.Echec("Photo introuvable.");
        p.Legende = Nettoyer(legende, 200);
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> OrdonnerPhotosAsync(int serviceId, IReadOnlyList<int> ordre, CancellationToken ct = default)
    {
        var photos = await db.ServicePhotos.Where(p => p.ServiceId == serviceId).ToListAsync(ct);
        if (ordre.Count != photos.Count || !photos.Select(p => p.Id).ToHashSet().SetEquals(ordre))
            return ResultatOperation.Echec("La liste des photos a changé entre-temps. Rechargez la page.");
        var index = photos.ToDictionary(p => p.Id);
        for (var i = 0; i < ordre.Count; i++) index[ordre[i]].Ordre = i;
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> DeplacerPhotoAsync(int serviceId, int photoId, int decalage, CancellationToken ct = default)
    {
        var ids = await db.ServicePhotos.Where(p => p.ServiceId == serviceId).OrderBy(p => p.Ordre).ThenBy(p => p.Id).Select(p => p.Id).ToListAsync(ct);
        var i = ids.IndexOf(photoId);
        if (i < 0) return ResultatOperation.Echec("Photo introuvable.");
        var j = Math.Clamp(i + Math.Sign(decalage), 0, ids.Count - 1);
        (ids[i], ids[j]) = (ids[j], ids[i]);
        return await OrdonnerPhotosAsync(serviceId, ids, ct);
    }

    public async Task<ResultatOperation> SupprimerPhotoAsync(int serviceId, int photoId, CancellationToken ct = default)
    {
        var p = await db.ServicePhotos.FirstOrDefaultAsync(x => x.Id == photoId && x.ServiceId == serviceId, ct);
        if (p is null) return ResultatOperation.Echec("Photo introuvable.");
        db.ServicePhotos.Remove(p);
        await db.SaveChangesAsync(ct);
        await Supprimer(p.CleStockage);
        if (p.CleVignette is not null) await Supprimer(p.CleVignette);
        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(ServicePhoto), photoId.ToString(), $"Service n° {serviceId} : suppression d'une photo", ct: ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    private async Task<IReadOnlyList<PhotoAdminVm>> PhotosAsync(int serviceId, CancellationToken ct)
    {
        var photos = await db.ServicePhotos.AsNoTracking().Where(p => p.ServiceId == serviceId).OrderBy(p => p.Ordre).ThenBy(p => p.Id).ToListAsync(ct);
        return photos.Select((p, i) => VersVm(p, i == 0)).ToList();
    }

    private PhotoAdminVm VersVm(ServicePhoto p, bool couverture) => new()
    {
        Id = p.Id,
        UrlVignette = p.CleVignette is not null ? stockage.UrlImage(p.CleVignette) : stockage.UrlImage(p.CleStockage, 480),
        UrlGrande = stockage.UrlImage(p.CleStockage),
        Largeur = p.Largeur, Hauteur = p.Hauteur, TailleOctets = p.TailleOctets, CouleurDominante = p.CouleurDominante,
        Legende = p.Legende, Ordre = p.Ordre, EstCouverture = couverture
    };

    private async Task Supprimer(string cle)
    {
        try { await stockage.SupprimerAsync(cle, ZoneStockage.Publique); }
        catch (Exception ex) { logger.LogWarning(ex, "Fichier {Cle} non supprimé.", cle); }
    }

    private static string? Nettoyer(string? s, int max = int.MaxValue) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > max ? s.Trim()[..max] : s.Trim());
}
