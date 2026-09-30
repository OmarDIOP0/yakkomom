using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Services;

public class Visite360Service(
    YakkomomDbContext db,
    IStorageService stockage,
    IJournalService journal,
    ILogger<Visite360Service> logger) : IVisite360Service
{
    public const int PanoramasMax = 12;
    public const int HotspotsMax = 20;

    // Limites des fichiers (déjà préparés par le navigateur)
    private const long HdTailleMax = 6 * 1024 * 1024, BdTailleMax = 1536 * 1024, VignetteTailleMax = 200 * 1024;
    private const int HdLargeurMax = 8192, BdLargeurMax = 3000, VignetteLargeurMax = 1024;
    private static readonly string[] Types = ["image/webp", "image/jpeg"];

    // =====================================================================
    // Panoramas
    // =====================================================================
    public async Task<IReadOnlyList<PanoramaAdminVm>> ListerAsync(int terrainId, CancellationToken ct = default)
    {
        var panoramas = await db.Panoramas.AsNoTracking().Where(p => p.TerrainId == terrainId)
            .OrderBy(p => p.Ordre).ThenBy(p => p.Id)
            .Select(p => new { P = p, N = p.Hotspots.Count }).ToListAsync(ct);
        return panoramas.Select(x => VersVm(x.P, x.N)).ToList();
    }

    public async Task<ResultatMedia<PanoramaAdminVm>> AjouterAsync(int terrainId, IFormFile? hd, IFormFile? bd, IFormFile? vignette,
        EnvoiPanorama infos, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == terrainId, ct);
        if (terrain is null) return ResultatMedia<PanoramaAdminVm>.Echec("Terrain introuvable.");

        if (infos.IdEnvoi is { } idEnvoi &&
            await db.Panoramas.AsNoTracking().FirstOrDefaultAsync(p => p.TerrainId == terrainId && p.IdEnvoi == idEnvoi, ct) is { } existant)
            return ResultatMedia<PanoramaAdminVm>.Ok(VersVm(existant, 0));

        if (await db.Panoramas.CountAsync(p => p.TerrainId == terrainId, ct) >= PanoramasMax)
            return ResultatMedia<PanoramaAdminVm>.Echec($"Maximum {PanoramasMax} panoramas par terrain.");

        var (infoHd, erreur) = await ValiderAsync(hd, HdTailleMax, 1024, HdLargeurMax, "Version HD", ct);
        if (erreur is not null) return ResultatMedia<PanoramaAdminVm>.Echec(erreur);
        var ratio = infoHd!.Largeur!.Value / (double)infoHd.Hauteur!.Value;
        if (ratio is < 1.9 or > 8)
            return ResultatMedia<PanoramaAdminVm>.Echec("Ce n'est pas un panorama : l'image doit être au moins deux fois plus large que haute (photo sphère ou mode panorama).");

        var (infoBd, erreurBd) = await ValiderAsync(bd, BdTailleMax, 512, BdLargeurMax, "Version légère", ct);
        if (erreurBd is not null) return ResultatMedia<PanoramaAdminVm>.Echec(erreurBd);
        var (infoVignette, erreurV) = await ValiderAsync(vignette, VignetteTailleMax, 120, VignetteLargeurMax, "Vignette", ct);
        if (erreurV is not null) return ResultatMedia<PanoramaAdminVm>.Echec(erreurV);

        var nom = Guid.NewGuid().ToString("N");
        var dossier = $"terrains/{terrainId}/panoramas/{nom}";
        var cles = new List<string>();
        try
        {
            async Task<string> Stocker(IFormFile f, InfosFichier info, string suffixe)
            {
                var cle = $"{dossier}-{suffixe}{info.Extension}";
                await using var flux = f.OpenReadStream();
                await stockage.EnregistrerAsync(flux, cle, info.TypeMime, ZoneStockage.Publique, ct);
                cles.Add(cle);
                return cle;
            }

            var cleHd = await Stocker(hd!, infoHd, "hd");
            var cleBd = await Stocker(bd!, infoBd!, "bd");
            var cleVignette = await Stocker(vignette!, infoVignette!, "vignette");

            var ordre = await db.Panoramas.Where(p => p.TerrainId == terrainId).MaxAsync(p => (int?)p.Ordre, ct) ?? -1;
            var panorama = new Panorama
            {
                TerrainId = terrainId,
                Titre = Nettoyer(infos.Titre, 120) ?? $"Vue {ordre + 2}",
                CleStockageHd = cleHd, TailleOctetsHd = hd!.Length,
                CleStockageBd = cleBd, TailleOctetsBd = bd!.Length,
                CleVignette = cleVignette,
                Largeur = infoHd.Largeur.Value, Hauteur = infoHd.Hauteur.Value,
                // Photo sphère : 180° ; mode panorama du téléphone : bande horizontale moins haute
                Vaov = Math.Round(Math.Min(180, 360.0 / ratio), 2),
                IdEnvoi = infos.IdEnvoi,
                Ordre = ordre + 1,
                EstDepart = !await db.Panoramas.AnyAsync(p => p.TerrainId == terrainId && p.EstDepart, ct)
            };
            db.Panoramas.Add(panorama);
            terrain.ModifieLe = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await journal.EnregistrerAsync(TypeAction.Creation, nameof(Panorama), panorama.Id.ToString(),
                $"{terrain.Reference} : ajout du panorama « {panorama.Titre} »", ct: ct);
            return ResultatMedia<PanoramaAdminVm>.Ok(VersVm(panorama, 0));
        }
        catch (Exception ex) when (ex is DbUpdateException or IOException)
        {
            logger.LogWarning(ex, "Ajout du panorama impossible, nettoyage des fichiers.");
            foreach (var cle in cles) await SupprimerFichierAsync(cle);
            return ResultatMedia<PanoramaAdminVm>.Echec("Enregistrement impossible, réessayez.");
        }
    }

    public async Task<ResultatOperation> RenommerAsync(int terrainId, int panoramaId, string? titre, CancellationToken ct = default)
    {
        var p = await Trouver(terrainId, panoramaId, ct);
        if (p is null) return ResultatOperation.Echec("Panorama introuvable.");
        p.Titre = Nettoyer(titre, 120) ?? p.Titre;
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> DefinirDepartAsync(int terrainId, int panoramaId, CancellationToken ct = default)
    {
        var tous = await db.Panoramas.Where(p => p.TerrainId == terrainId).ToListAsync(ct);
        var cible = tous.FirstOrDefault(p => p.Id == panoramaId);
        if (cible is null) return ResultatOperation.Echec("Panorama introuvable.");
        if (cible.EstDepart) return ResultatOperation.Ok();

        // Index unique « une seule scène de départ » : deux étapes dans une transaction.
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            foreach (var p in tous.Where(p => p.EstDepart)) p.EstDepart = false;
            await db.SaveChangesAsync(ct);
            cible.EstDepart = true;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> DeplacerAsync(int terrainId, int panoramaId, int decalage, CancellationToken ct = default)
    {
        var liste = await db.Panoramas.Where(p => p.TerrainId == terrainId).OrderBy(p => p.Ordre).ThenBy(p => p.Id).ToListAsync(ct);
        var i = liste.FindIndex(p => p.Id == panoramaId);
        if (i < 0) return ResultatOperation.Echec("Panorama introuvable.");
        var j = Math.Clamp(i + Math.Sign(decalage), 0, liste.Count - 1);
        (liste[i], liste[j]) = (liste[j], liste[i]);
        for (var k = 0; k < liste.Count; k++) liste[k].Ordre = k;
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> SupprimerAsync(int terrainId, int panoramaId, CancellationToken ct = default)
    {
        var p = await Trouver(terrainId, panoramaId, ct);
        if (p is null) return ResultatOperation.Echec("Panorama introuvable.");

        db.Panoramas.Remove(p); // les points de passage vers/depuis ce panorama sont supprimés en cascade
        await db.SaveChangesAsync(ct);

        if (p.EstDepart && await db.Panoramas.Where(x => x.TerrainId == terrainId).OrderBy(x => x.Ordre).FirstOrDefaultAsync(ct) is { } suivant)
        {
            suivant.EstDepart = true;
            await db.SaveChangesAsync(ct);
        }
        foreach (var cle in new[] { p.CleStockageHd, p.CleStockageBd, p.CleVignette }.OfType<string>())
            await SupprimerFichierAsync(cle);

        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(Panorama), panoramaId.ToString(), $"Suppression du panorama « {p.Titre} »", ct: ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    // Vue initiale et points de passage
    // =====================================================================
    public async Task<ResultatOperation> EnregistrerVueAsync(int terrainId, int panoramaId, VueInitiale vue, CancellationToken ct = default)
    {
        var p = await Trouver(terrainId, panoramaId, ct);
        if (p is null) return ResultatOperation.Echec("Panorama introuvable.");
        if (!double.IsFinite(vue.Yaw) || !double.IsFinite(vue.Pitch) || !double.IsFinite(vue.Hfov)) return ResultatOperation.Echec("Vue invalide.");

        p.YawInitial = Math.Round(NormaliserYaw(vue.Yaw), 2);
        p.PitchInitial = Math.Round(Math.Clamp(vue.Pitch, -90, 90), 2);
        p.HfovInitial = Math.Round(Math.Clamp(vue.Hfov, 40, 120), 2);
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatMedia<int>> AjouterHotspotAsync(int terrainId, int panoramaId, NouveauHotspot h, CancellationToken ct = default)
    {
        var p = await Trouver(terrainId, panoramaId, ct);
        if (p is null) return ResultatMedia<int>.Echec("Panorama introuvable.");
        if (!Enum.IsDefined(h.Type) || !double.IsFinite(h.Pitch) || !double.IsFinite(h.Yaw)) return ResultatMedia<int>.Echec("Point invalide.");
        if (await db.Hotspots.CountAsync(x => x.PanoramaId == panoramaId, ct) >= HotspotsMax)
            return ResultatMedia<int>.Echec($"Maximum {HotspotsMax} points par panorama.");

        string? texte = Nettoyer(h.Texte, 200);
        if (h.Type == TypeHotspot.Navigation)
        {
            var cible = h.CibleId is { } id ? await Trouver(terrainId, id, ct) : null;
            if (cible is null || cible.Id == panoramaId) return ResultatMedia<int>.Echec("Choisissez le panorama de destination.");
            texte ??= $"Vers : {cible.Titre}";
        }
        else if (texte is null) return ResultatMedia<int>.Echec("Saisissez le texte de l'information.");

        var hotspot = new Hotspot
        {
            PanoramaId = panoramaId,
            Type = h.Type,
            PanoramaCibleId = h.Type == TypeHotspot.Navigation ? h.CibleId : null,
            Pitch = Math.Round(Math.Clamp(h.Pitch, -90, 90), 2),
            Yaw = Math.Round(NormaliserYaw(h.Yaw), 2),
            Texte = texte
        };
        db.Hotspots.Add(hotspot);
        await db.SaveChangesAsync(ct);
        return ResultatMedia<int>.Ok(hotspot.Id);
    }

    public async Task<ResultatOperation> SupprimerHotspotAsync(int terrainId, int hotspotId, CancellationToken ct = default)
    {
        var h = await db.Hotspots.Include(x => x.Panorama).FirstOrDefaultAsync(x => x.Id == hotspotId && x.Panorama.TerrainId == terrainId, ct);
        if (h is null) return ResultatOperation.Echec("Point introuvable.");
        db.Hotspots.Remove(h);
        await db.SaveChangesAsync(ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    // Configuration Pannellum
    // =====================================================================
    public async Task<string?> ConfigurationAsync(int terrainId, bool hd, bool edition, CancellationToken ct = default)
    {
        var panoramas = await db.Panoramas.AsNoTracking().Include(p => p.Hotspots)
            .Where(p => p.TerrainId == terrainId).OrderBy(p => p.Ordre).ToListAsync(ct);
        if (panoramas.Count == 0) return null;

        var depart = panoramas.FirstOrDefault(p => p.EstDepart) ?? panoramas[0];
        var scenes = panoramas.ToDictionary(p => IdScene(p.Id), p => (object)new Dictionary<string, object?>
        {
            ["title"] = p.Titre,
            ["type"] = "equirectangular",
            ["panorama"] = stockage.UrlImage(hd ? p.CleStockageHd : p.CleStockageBd ?? p.CleStockageHd),
            ["yaw"] = p.YawInitial,
            ["pitch"] = p.PitchInitial,
            ["hfov"] = p.HfovInitial,
            ["haov"] = 360,
            ["vaov"] = p.Vaov,
            ["avoidShowingBackground"] = p.Vaov < 180,
            ["hotSpots"] = p.Hotspots.OrderBy(h => h.Id).Select(h => (object)(h.Type == TypeHotspot.Navigation && h.PanoramaCibleId is { } cible
                ? new Dictionary<string, object?> { ["id"] = h.Id, ["pitch"] = h.Pitch, ["yaw"] = h.Yaw, ["type"] = "scene", ["text"] = h.Texte, ["sceneId"] = IdScene(cible) }
                : new Dictionary<string, object?> { ["id"] = h.Id, ["pitch"] = h.Pitch, ["yaw"] = h.Yaw, ["type"] = "info", ["text"] = h.Texte })).ToList()
        });

        var config = new Dictionary<string, object?>
        {
            ["default"] = new Dictionary<string, object?>
            {
                ["firstScene"] = IdScene(depart.Id),
                ["sceneFadeDuration"] = 600,
                ["autoLoad"] = true,
                ["showZoomCtrl"] = true,
                ["showFullscreenCtrl"] = true,
                ["orientationOnByDefault"] = false,
                ["compass"] = false,
                ["hotSpotDebug"] = edition,
                ["strings"] = TextesFrancais
            },
            ["scenes"] = scenes
        };
        return JsonSerializer.Serialize(config);
    }

    public async Task<VisitePubliqueVm?> VisitePubliqueAsync(int terrainId, CancellationToken ct = default)
    {
        var panoramas = await db.Panoramas.AsNoTracking().Where(p => p.TerrainId == terrainId).OrderBy(p => p.Ordre).ToListAsync(ct);
        if (panoramas.Count == 0) return null;
        var depart = panoramas.FirstOrDefault(p => p.EstDepart) ?? panoramas[0];

        return new VisitePubliqueVm
        {
            UrlVignette = stockage.UrlImage(depart.CleVignette ?? depart.CleStockageBd ?? depart.CleStockageHd, 960),
            TitreDepart = depart.Titre,
            NombreScenes = panoramas.Count,
            TailleLancement = depart.TailleOctetsBd > 0 ? depart.TailleOctetsBd : depart.TailleOctetsHd,
            TailleHdDepart = depart.TailleOctetsHd,
            ConfigJson = (await ConfigurationAsync(terrainId, hd: false, edition: false, ct))!,
            UrlsHdJson = JsonSerializer.Serialize(panoramas.ToDictionary(p => IdScene(p.Id), p => stockage.UrlImage(p.CleStockageHd)))
        };
    }

    // =====================================================================
    private static readonly Dictionary<string, string> TextesFrancais = new()
    {
        ["loadButtonLabel"] = "Cliquez pour<br>charger la visite",
        ["loadingLabel"] = "Chargement…",
        ["bylineLabel"] = "",
        ["noPanoramaError"] = "Aucune image de panorama.",
        ["fileAccessError"] = "Le fichier %s est inaccessible.",
        ["malformedURLError"] = "Adresse du panorama invalide.",
        ["iOS8WebGLError"] = "Votre appareil ne permet pas d'afficher la visite 360°.",
        ["genericWebGLError"] = "Votre appareil ne permet pas d'afficher la visite 360°.",
        ["textureSizeError"] = "Ce panorama est trop grand pour votre appareil. Essayez la version légère.",
        ["unknownError"] = "Erreur inconnue. Réessayez."
    };

    public static string IdScene(int panoramaId) => $"s{panoramaId}";

    private static double NormaliserYaw(double yaw) => ((yaw + 180) % 360 + 360) % 360 - 180;

    private Task<Panorama?> Trouver(int terrainId, int panoramaId, CancellationToken ct) =>
        db.Panoramas.FirstOrDefaultAsync(p => p.Id == panoramaId && p.TerrainId == terrainId, ct);

    private PanoramaAdminVm VersVm(Panorama p, int nombreHotspots) => new()
    {
        Id = p.Id, Titre = p.Titre,
        UrlVignette = stockage.UrlImage(p.CleVignette ?? p.CleStockageBd ?? p.CleStockageHd, 640),
        TailleHd = p.TailleOctetsHd, TailleBd = p.TailleOctetsBd, Largeur = p.Largeur, Hauteur = p.Hauteur, Vaov = p.Vaov,
        EstDepart = p.EstDepart, Ordre = p.Ordre, NombreHotspots = nombreHotspots
    };

    private static async Task<(InfosFichier?, string?)> ValiderAsync(IFormFile? f, long tailleMax, int largeurMin, int largeurMax, string nom, CancellationToken ct)
    {
        if (f is null || f.Length == 0) return (null, $"{nom} : fichier manquant.");
        if (f.Length > tailleMax) return (null, $"{nom} : fichier trop lourd (maximum {ReglesEnvoi.TailleLisible(tailleMax)}).");
        InfosFichier? info;
        await using (var flux = f.OpenReadStream()) info = await AnalyseFichier.AnalyserAsync(flux, ct);
        if (info is null || !Types.Contains(info.TypeMime) || info.Largeur is null || info.Hauteur is null)
            return (null, $"{nom} : format non accepté (WebP ou JPEG).");
        if (info.Largeur < largeurMin || info.Largeur > largeurMax)
            return (null, $"{nom} : largeur {info.Largeur} px hors limites ({largeurMin}–{largeurMax} px).");
        return (info, null);
    }

    private async Task SupprimerFichierAsync(string cle)
    {
        try { await stockage.SupprimerAsync(cle, ZoneStockage.Publique); }
        catch (Exception ex) { logger.LogWarning(ex, "Fichier {Cle} non supprimé.", cle); }
    }

    private static string? Nettoyer(string? s, int max) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > max ? s.Trim()[..max] : s.Trim());
}
