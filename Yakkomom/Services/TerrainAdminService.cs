using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

public class TerrainAdminService(
    YakkomomDbContext db,
    IParametreSiteService parametres,
    ILocaliteService localites,
    IJournalService journal,
    IMediaService medias,
    IHttpContextAccessor http) : ITerrainAdminService
{
    public const int TaillePage = 20;

    // Limites raisonnables pour détecter les fautes de frappe
    private const long PrixMax = 100_000_000_000;      // 100 milliards FCFA
    private const decimal SurfaceMax = 100_000_000m;   // 10 000 ha
    private const decimal DimensionMax = 100_000m;     // 100 km

    private string? UtilisateurId => http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    // =====================================================================
    // Liste
    // =====================================================================
    public async Task<ListeTerrainsAdminVm> ListerAsync(FiltreTerrainsAdmin filtre, CancellationToken ct = default)
    {
        var requete = db.Terrains.AsNoTracking();

        if (filtre.Statut is { } statut) requete = requete.Where(t => t.Statut == statut);
        if (!string.IsNullOrWhiteSpace(filtre.Q))
        {
            var motif = "%" + filtre.Q.Trim().Replace("%", "").Replace("_", "") + "%";
            requete = requete.Where(t =>
                EF.Functions.ILike(t.Reference, motif) || EF.Functions.ILike(t.Titre, motif) ||
                (t.QuartierVillage != null && EF.Functions.ILike(t.QuartierVillage, motif)) ||
                (t.Commune != null && EF.Functions.ILike(t.Commune.Nom, motif)));
        }

        var vms = (await LignesAsync(requete, ct))
            .Where(v => !filtre.Incomplets || !v.Completude.EstComplete)
            .ToList();

        var page = Math.Max(1, filtre.Page);
        var total = vms.Count;
        var elements = vms.Skip((page - 1) * TaillePage).Take(TaillePage).ToList();

        var comptes = await db.Terrains.AsNoTracking().GroupBy(t => t.Statut)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.N, ct);

        return new ListeTerrainsAdminVm
        {
            Filtre = filtre,
            Resultats = new PageResultat<TerrainLigneVm>(elements, total, page, TaillePage),
            CompteParStatut = comptes
        };
    }

    public async Task<(IReadOnlyList<TerrainLigneVm> Terrains, int Total)> ListerACompleterAsync(int maximum, CancellationToken ct = default)
    {
        // Les fiches visibles du public passent d'abord, puis les brouillons ; vendus et archivés sont ignorés.
        var requete = db.Terrains.AsNoTracking().Where(t =>
            t.Statut == StatutTerrain.Disponible || t.Statut == StatutTerrain.Reserve || t.Statut == StatutTerrain.Brouillon);
        var incomplets = (await LignesAsync(requete, ct)).Where(v => !v.Completude.EstComplete)
            .OrderBy(v => v.Statut == StatutTerrain.Brouillon)
            .ThenBy(v => v.Completude.Pourcentage)
            .ThenByDescending(v => v.ModifieLe)
            .ToList();
        return (incomplets.Take(maximum).ToList(), incomplets.Count);
    }

    /// <summary>Projection légère ; la complétude se calcule en mémoire (quelques centaines de terrains au plus).</summary>
    private async Task<List<TerrainLigneVm>> LignesAsync(IQueryable<Terrain> requete, CancellationToken ct)
    {
        var contactsDefaut = (await parametres.ObtenirAsync(ct)).ContactsParDefaut;
        var lignes = await requete
            .OrderByDescending(t => t.ModifieLe)
            .Select(t => new
            {
                t.Id, t.Reference, t.Titre, t.Statut, t.Prix, t.SurfaceM2, t.ModifieLe, t.EstMisEnAvant,
                Commune = t.Commune != null ? t.Commune.Nom : null,
                Entree = new EntreeCompletude(t.Type, t.Prix, t.SurfaceM2, t.Description, t.CommuneId, t.Latitude, t.Longitude,
                    t.Photos.Count, t.SituationFonciere, t.Documents.Count, t.AccesEau, t.AccesElectricite, t.RouteAcces,
                    t.Contacts.WhatsApp1 != null || t.Contacts.WhatsApp2 != null || t.Contacts.WhatsApp3 != null)
            })
            .ToListAsync(ct);

        var aContactDefaut = EntreeCompletude.ContactAvecWhatsApp(contactsDefaut);
        return lignes.Select(l => new TerrainLigneVm
        {
            Id = l.Id, Reference = l.Reference, Titre = l.Titre, Statut = l.Statut, Prix = l.Prix,
            SurfaceM2 = l.SurfaceM2, Commune = l.Commune, ModifieLe = l.ModifieLe, EstMisEnAvant = l.EstMisEnAvant,
            Completude = Completude.Calculer(l.Entree with { AUnWhatsApp = l.Entree.AUnWhatsApp || aContactDefaut })
        }).ToList();
    }

    // =====================================================================
    // Lecture
    // =====================================================================
    public Task<Terrain?> ObtenirAsync(int id, CancellationToken ct = default) =>
        db.Terrains.AsNoTracking().AsSplitQuery()
            .Include(t => t.Photos).Include(t => t.Documents).Include(t => t.Commune)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<EnTeteTerrainVm?> EnTeteAsync(int id, OngletTerrain onglet, CancellationToken ct = default)
    {
        var t = await ObtenirAsync(id, ct);
        if (t is null) return null;
        var contactsDefaut = (await parametres.ObtenirAsync(ct)).ContactsParDefaut;
        return new EnTeteTerrainVm
        {
            Id = t.Id, Reference = t.Reference, Titre = t.Titre, Statut = t.Statut, ModifieLe = t.ModifieLe,
            UrlPublique = UrlTerrain.Chemin(t.Reference, t.SurfaceM2, t.Commune?.Nom),
            Completude = Completude.Calculer(EntreeCompletude.Depuis(t, contactsDefaut)),
            OngletActif = onglet
        };
    }

    // =====================================================================
    // Création
    // =====================================================================
    public async Task<Terrain> CreerBrouillonAsync(string titre, CancellationToken ct = default)
    {
        var numero = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{YakkomomDbContext.SequenceReferenceTerrain}') AS \"Value\"")
            .SingleAsync(ct);

        var terrain = new Terrain
        {
            Reference = ReferenceTerrain.Formater(numero),
            Titre = titre.Trim(),
            Statut = StatutTerrain.Brouillon,
            CreeParId = UtilisateurId,
            ModifieParId = UtilisateurId
        };
        db.Terrains.Add(terrain);
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Creation, nameof(Terrain), terrain.Id.ToString(),
            $"Création du brouillon {terrain.Reference} « {terrain.Titre} »", ct: ct);
        return terrain;
    }

    // =====================================================================
    // Onglets
    // =====================================================================
    public async Task<ResultatOperation> EnregistrerInfosAsync(int id, InfosTerrainVm vm, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");

        var r = new ResultatOperation();
        if (!Nombres.LireMontant(vm.Prix, out var prix) || prix > PrixMax)
            r.AjouterErreur(nameof(vm.Prix), "Prix invalide : saisissez un montant en FCFA, ex. 12 500 000.");
        var surface = LireDecimalPositif(vm.SurfaceM2, nameof(vm.SurfaceM2), "Surface invalide (en m²), ex. 500 ou 1 250,5.", SurfaceMax, r);
        var longueur = LireDecimalPositif(vm.LongueurM, nameof(vm.LongueurM), "Longueur invalide (en mètres).", DimensionMax, r);
        var largeur = LireDecimalPositif(vm.LargeurM, nameof(vm.LargeurM), "Largeur invalide (en mètres).", DimensionMax, r);
        if (vm.Type is { } type && !Enum.IsDefined(type)) r.AjouterErreur(nameof(vm.Type), "Type inconnu.");
        if (!r.Reussi) return r;

        terrain.Titre = vm.Titre.Trim();
        terrain.Type = vm.Type;
        terrain.Description = Nettoyer(vm.Description);
        terrain.Prix = prix;
        terrain.PrixNegociable = vm.PrixNegociable;
        terrain.SurfaceM2 = surface;
        terrain.LongueurM = longueur;
        terrain.LargeurM = largeur;
        terrain.AccesEau = vm.AccesEau;
        terrain.AccesElectricite = vm.AccesElectricite;
        terrain.RouteAcces = vm.RouteAcces;
        terrain.RouteAccesDetail = Nettoyer(vm.RouteAccesDetail);
        terrain.EstMisEnAvant = vm.EstMisEnAvant;

        return await EnregistrerAsync(terrain, "informations générales", ct);
    }

    public async Task<ResultatOperation> EnregistrerLocalisationAsync(int id, LocalisationTerrainVm vm, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");

        var r = new ResultatOperation();
        var (regionId, departementId, communeId, erreurLieu) = await localites.ResoudreAsync(vm.RegionId, vm.DepartementId, vm.CommuneId, ct);
        if (erreurLieu is not null) r.AjouterErreur(nameof(vm.CommuneId), erreurLieu);

        if (!Nombres.LireCoordonnee(vm.Latitude, out var lat) || lat is < Geo.LatMin or > Geo.LatMax)
            r.AjouterErreur(nameof(vm.Latitude), "Latitude invalide : au Sénégal elle est comprise entre 12 et 17 (ex. 14.5198).");
        if (!Nombres.LireCoordonnee(vm.Longitude, out var lng) || lng is < Geo.LngMin or > Geo.LngMax)
            r.AjouterErreur(nameof(vm.Longitude), "Longitude invalide : au Sénégal elle est négative, entre -18 et -11 (ex. -17.0021).");
        if ((lat is null) != (lng is null))
            r.AjouterErreur(nameof(vm.Latitude), "Renseignez la latitude ET la longitude, ou aucune des deux.");

        if (!Geo.LirePolygone(vm.ContourGeoJson, out var contour, out var erreurContour))
            r.AjouterErreur(nameof(vm.ContourGeoJson), erreurContour!);

        var commodites = new List<Commodite>();
        for (var i = 0; i < vm.Commodites.Count; i++)
        {
            var c = vm.Commodites[i];
            if (string.IsNullOrWhiteSpace(c.Libelle) && string.IsNullOrWhiteSpace(c.DistanceKm)) continue;
            if (string.IsNullOrWhiteSpace(c.Libelle))
            {
                r.AjouterErreur($"Commodites[{i}].Libelle", "Indiquez le nom de la commodité.");
                continue;
            }
            if (!Nombres.LireDecimal(c.DistanceKm, out var km) || km is < 0 or > 500)
            {
                r.AjouterErreur($"Commodites[{i}].DistanceKm", "Distance invalide (en km), ex. 1,5.");
                continue;
            }
            commodites.Add(new Commodite { Libelle = c.Libelle.Trim(), DistanceKm = km });
        }
        if (!r.Reussi) return r;

        terrain.RegionId = regionId;
        terrain.DepartementId = departementId;
        terrain.CommuneId = communeId;
        terrain.QuartierVillage = Nettoyer(vm.QuartierVillage);
        terrain.Adresse = Nettoyer(vm.Adresse);
        // Contour sans point : le repère se place au centre de la parcelle.
        if (contour.Count >= 3 && lat is null && lng is null)
        {
            var centre = Geo.Centre(contour);
            lat = Math.Round(centre.Lat, 7);
            lng = Math.Round(centre.Lng, 7);
        }

        terrain.Latitude = lat;
        terrain.Longitude = lng;
        terrain.ContourGeoJson = contour.Count >= 3 ? Geo.VersGeoJson(contour) : null;
        terrain.SurfaceCalculeeM2 = contour.Count >= 3 ? Math.Round((decimal)Geo.AireM2(contour), 0) : null;
        terrain.Commodites = commodites;

        return await EnregistrerAsync(terrain, "localisation", ct);
    }

    public async Task<ResultatOperation> EnregistrerMediasAsync(int id, MediasTerrainVm vm, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");

        string? url = null;
        if (!string.IsNullOrWhiteSpace(vm.VideoYoutubeUrl))
        {
            var idVideo = YouTube.ExtraireId(vm.VideoYoutubeUrl);
            if (idVideo is null)
                return new ResultatOperation().AjouterErreur(nameof(vm.VideoYoutubeUrl),
                    "Lien YouTube non reconnu. Copiez le lien depuis le bouton « Partager » de YouTube.");
            url = YouTube.UrlCanonique(idVideo);
        }
        terrain.VideoYoutubeUrl = url;
        return await EnregistrerAsync(terrain, "médias", ct);
    }

    public async Task<ResultatOperation> EnregistrerDocumentsAsync(int id, DocumentsTerrainVm vm, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");
        if (vm.SituationFonciere is { } s && !Enum.IsDefined(s))
            return new ResultatOperation().AjouterErreur(nameof(vm.SituationFonciere), "Type de document inconnu.");

        terrain.SituationFonciere = vm.SituationFonciere;
        return await EnregistrerAsync(terrain, "situation foncière", ct);
    }

    public async Task<ResultatOperation> EnregistrerContactsAsync(int id, ContactsTerrainVm vm, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");

        var r = ContactsSaisie.Appliquer(terrain.Contacts,
            (vm.WhatsApp1, vm.WhatsApp1Libelle), (vm.WhatsApp2, vm.WhatsApp2Libelle), (vm.WhatsApp3, vm.WhatsApp3Libelle), vm.Email);
        return r.Reussi ? await EnregistrerAsync(terrain, "contacts", ct) : r;
    }

    // =====================================================================
    // Statut et suppression
    // =====================================================================
    public async Task<ResultatOperation> ChangerStatutAsync(int id, StatutTerrain statut, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(statut)) return ResultatOperation.Echec("Statut inconnu.");
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");
        if (terrain.Statut == statut) return ResultatOperation.Ok();

        var ancien = terrain.Statut;
        terrain.Statut = statut;
        if (terrain.EstPublic && terrain.PublieLe is null) terrain.PublieLe = DateTime.UtcNow;
        Tracer(terrain);
        await db.SaveChangesAsync(ct);

        await journal.EnregistrerAsync(TypeAction.ChangementStatut, nameof(Terrain), id.ToString(),
            $"{terrain.Reference} : {Format.Statut(ancien)} → {Format.Statut(statut)}", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> SupprimerAsync(int id, CancellationToken ct = default)
    {
        var terrain = await db.Terrains.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (terrain is null) return ResultatOperation.Echec("Terrain introuvable.");

        // Fichiers d'abord (photos, documents) : la base garde la trace si le stockage échoue en cours.
        await medias.SupprimerFichiersTerrainAsync(id, ct);
        db.Terrains.Remove(terrain);
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(Terrain), id.ToString(),
            $"Suppression de {terrain.Reference} « {terrain.Titre} »", ct: ct);
        return ResultatOperation.Ok();
    }

    // =====================================================================
    // Outils
    // =====================================================================
    private async Task<ResultatOperation> EnregistrerAsync(Terrain terrain, string section, CancellationToken ct)
    {
        var modifies = ChampsModifies(db.Entry(terrain));
        if (modifies.Count == 0) return ResultatOperation.Ok();

        Tracer(terrain);
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(Terrain), terrain.Id.ToString(),
            $"{terrain.Reference} : modification ({section})", new { champs = modifies }, ct);
        return ResultatOperation.Ok();
    }

    private void Tracer(Terrain terrain)
    {
        terrain.ModifieLe = DateTime.UtcNow;
        terrain.ModifieParId = UtilisateurId;
    }

    /// <summary>Noms des propriétés réellement modifiées (pour le journal).</summary>
    private static List<string> ChampsModifies(EntityEntry entree)
    {
        entree.DetectChanges();
        var champs = entree.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToList();

        // Types possédés : Contacts (colonnes) et Commodites (JSON)
        foreach (var reference in entree.References.Where(r => r.TargetEntry is not null && r.Metadata.TargetEntityType.IsOwned()))
            champs.AddRange(reference.TargetEntry!.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name));
        if (entree.Context.ChangeTracker.Entries<Commodite>().Any(e => e.State != EntityState.Unchanged))
            champs.Add(nameof(Terrain.Commodites));

        return champs.Distinct().ToList();
    }

    private static decimal? LireDecimalPositif(string? saisie, string champ, string message, decimal max, ResultatOperation r)
    {
        if (!Nombres.LireDecimal(saisie, out var v) || v is <= 0 || v > max)
        {
            r.AjouterErreur(champ, message);
            return null;
        }
        return v;
    }

    private static string? Nettoyer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
