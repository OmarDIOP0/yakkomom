using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

public class TableauDeBordService(YakkomomDbContext db, ITerrainAdminService terrains) : ITableauDeBordService
{
    public const int TaillePageJournal = 50;
    private const int MaxTerrainsClics = 15;
    private const int MaxACompleter = 8;

    /// <summary>Libellés des types d'éléments journalisés (clé = <see cref="JournalAction.EntiteType"/>).</summary>
    public static readonly IReadOnlyList<(string Cle, string Libelle)> TypesEntites =
    [
        (nameof(Terrain), "Terrains"),
        (nameof(TerrainPhoto), "Photos de terrain"),
        (nameof(DocumentFoncier), "Documents fonciers"),
        (nameof(Panorama), "Visites 360°"),
        (nameof(Service), "Services"),
        (nameof(ServicePhoto), "Photos de service"),
        (nameof(Localite), "Localités"),
        (nameof(ParametreSite), "Paramètres du site"),
        ("Compte", "Comptes admin"),
    ];

    // =====================================================================
    // Tableau de bord
    // =====================================================================
    public async Task<TableauDeBordVm> TableauAsync(int jours, CancellationToken ct = default)
    {
        if (!TableauDeBordVm.Periodes.Contains(jours)) jours = 30;

        var parStatut = await db.Terrains.AsNoTracking().GroupBy(t => t.Statut)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.N, ct);
        var clics = await ClicsAsync(jours, ct);
        var (aCompleter, totalACompleter) = await terrains.ListerACompleterAsync(MaxACompleter, ct);
        var actions = await db.JournalActions.AsNoTracking()
            .Where(j => j.Action != TypeAction.Connexion)
            .OrderByDescending(j => j.Date).Take(6)
            .Select(VersLigne).ToListAsync(ct);
        await ResoudreLiensAsync(actions, voirComptes: false, ct);

        return new TableauDeBordVm
        {
            Jours = jours,
            TerrainsParStatut = parStatut,
            Clics = clics,
            ACompleter = aCompleter,
            TotalACompleter = totalACompleter,
            DernieresActions = actions
        };
    }

    private async Task<StatistiquesClicsVm> ClicsAsync(int jours, CancellationToken ct)
    {
        // Dakar est à UTC+0 toute l'année : un jour de Dakar = un jour UTC.
        var aujourdhui = DateOnly.FromDateTime(Format.HeureDakar(DateTime.UtcNow));
        var premierJour = aujourdhui.AddDays(-(jours - 1));
        var debut = premierJour.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var debutPrecedent = debut.AddDays(-jours);

        var periode = db.ClicsWhatsApp.AsNoTracking().Where(c => c.Date >= debut);

        var total = await periode.CountAsync(ct);
        var precedent = await db.ClicsWhatsApp.AsNoTracking().CountAsync(c => c.Date >= debutPrecedent && c.Date < debut, ct);

        var parJourBrut = await periode.GroupBy(c => c.Date.Date)
            .Select(g => new { Jour = g.Key, N = g.Count() }).ToListAsync(ct);
        var index = parJourBrut.ToDictionary(x => DateOnly.FromDateTime(x.Jour), x => x.N);
        var parJour = Enumerable.Range(0, jours)
            .Select(i => premierJour.AddDays(i))
            .Select(j => (j, index.GetValueOrDefault(j)))
            .ToList();

        var parSource = (await periode.GroupBy(c => c.Source)
                .Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct))
            .OrderByDescending(x => x.N).Select(x => (x.Key, x.N)).ToList();

        var comptesTerrains = await periode.Where(c => c.TerrainId != null)
            .GroupBy(c => c.TerrainId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Count(),
                Fiche = g.Count(c => c.Source == SourceClicWhatsApp.FicheTerrain),
                Liste = g.Count(c => c.Source == SourceClicWhatsApp.Liste),
                Flottant = g.Count(c => c.Source == SourceClicWhatsApp.BoutonFlottant)
            })
            .OrderByDescending(x => x.Total).ThenBy(x => x.Id)
            .Take(MaxTerrainsClics)
            .ToListAsync(ct);
        var idsTerrains = comptesTerrains.Select(x => x.Id).ToList();
        var infosTerrains = await db.Terrains.AsNoTracking().Where(t => idsTerrains.Contains(t.Id))
            .Select(t => new { t.Id, t.Reference, t.Titre, t.Statut }).ToDictionaryAsync(t => t.Id, ct);
        var parTerrain = comptesTerrains.Where(x => infosTerrains.ContainsKey(x.Id))
            .Select(x => new ClicsTerrainVm(x.Id, infosTerrains[x.Id].Reference, infosTerrains[x.Id].Titre, infosTerrains[x.Id].Statut,
                x.Total, x.Fiche, x.Liste, x.Flottant))
            .ToList();

        var comptesServices = await periode.Where(c => c.ServiceId != null)
            .GroupBy(c => c.ServiceId!.Value)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .OrderByDescending(x => x.N).Take(10).ToListAsync(ct);
        var idsServices = comptesServices.Select(x => x.Id).ToList();
        var titresServices = await db.Services.AsNoTracking().Where(sv => idsServices.Contains(sv.Id))
            .ToDictionaryAsync(sv => sv.Id, sv => sv.Titre, ct);
        var parService = comptesServices.Where(x => titresServices.ContainsKey(x.Id))
            .Select(x => (x.Id, titresServices[x.Id], x.N)).ToList();

        var generaux = await periode.CountAsync(c => c.TerrainId == null && c.ServiceId == null, ct);

        return new StatistiquesClicsVm
        {
            Total = total,
            TotalPrecedent = precedent,
            ParJour = parJour,
            ParSource = parSource,
            ParTerrain = parTerrain,
            ParService = parService,
            Generaux = generaux
        };
    }

    // =====================================================================
    // Journal
    // =====================================================================
    public async Task<JournalVm> JournalAsync(FiltreJournal filtre, bool voirComptes, CancellationToken ct = default)
    {
        var requete = db.JournalActions.AsNoTracking();

        if (!string.IsNullOrEmpty(filtre.Utilisateur)) requete = requete.Where(j => j.UtilisateurId == filtre.Utilisateur);
        if (filtre.Action is { } action) requete = requete.Where(j => j.Action == action);
        if (!string.IsNullOrEmpty(filtre.Entite)) requete = requete.Where(j => j.EntiteType == filtre.Entite);
        if (filtre.Du is { } du)
        {
            var d = du.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requete = requete.Where(j => j.Date >= d);
        }
        if (filtre.Au is { } au)
        {
            var a = au.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requete = requete.Where(j => j.Date < a);
        }
        if (!string.IsNullOrWhiteSpace(filtre.Q))
        {
            var motif = "%" + filtre.Q.Trim().Replace("%", "").Replace("_", "") + "%";
            requete = requete.Where(j => EF.Functions.ILike(j.Description, motif)
                || (j.UtilisateurNom != null && EF.Functions.ILike(j.UtilisateurNom, motif)));
        }

        var total = await requete.CountAsync(ct);
        var taille = TaillePageJournal;
        var nombrePages = Math.Max(1, (int)Math.Ceiling(total / (double)taille));
        var page = Math.Clamp(filtre.Page, 1, nombrePages);
        var lignes = await requete.OrderByDescending(j => j.Date).ThenByDescending(j => j.Id)
            .Skip((page - 1) * taille).Take(taille)
            .Select(VersLigne).ToListAsync(ct);
        await ResoudreLiensAsync(lignes, voirComptes, ct);

        // Auteurs présents dans le journal (le nom est une copie : il reste lisible si le compte disparaît).
        var utilisateurs = (await db.JournalActions.AsNoTracking()
                .Where(j => j.UtilisateurId != null)
                .GroupBy(j => j.UtilisateurId)
                .Select(g => new { Id = g.Key!, Nom = g.OrderByDescending(j => j.Date).Select(j => j.UtilisateurNom).First() })
                .ToListAsync(ct))
            .Select(u => (u.Id, u.Nom ?? "Compte supprimé"))
            .OrderBy(u => u.Item2, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new JournalVm
        {
            Filtre = filtre,
            Resultats = new PageResultat<JournalLigneVm>(lignes, total, page, taille),
            Utilisateurs = utilisateurs,
            Entites = TypesEntites
        };
    }

    private static readonly System.Linq.Expressions.Expression<Func<JournalAction, JournalLigneVm>> VersLigne = j => new JournalLigneVm
    {
        Id = j.Id, Date = j.Date, UtilisateurNom = j.UtilisateurNom, Action = j.Action,
        EntiteType = j.EntiteType, EntiteId = j.EntiteId, Description = j.Description, DetailsJson = j.DetailsJson
    };

    /// <summary>
    /// Lien vers l'élément concerné, s'il existe encore. Photos, documents et panoramas
    /// renvoient à l'onglet correspondant de leur terrain (une requête par type, pas par ligne).
    /// </summary>
    private async Task ResoudreLiensAsync(IReadOnlyList<JournalLigneVm> lignes, bool voirComptes, CancellationToken ct)
    {
        List<int> Ids(string type) => lignes.Where(l => l.EntiteType == type)
            .Select(l => int.TryParse(l.EntiteId, out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();

        var idsTerrains = Ids(nameof(Terrain));
        var idsPhotos = Ids(nameof(TerrainPhoto));
        var idsDocuments = Ids(nameof(DocumentFoncier));
        var idsPanoramas = Ids(nameof(Panorama));
        var idsServices = Ids(nameof(Service));
        var idsPhotosServices = Ids(nameof(ServicePhoto));

        var terrainsExistants = idsTerrains.Count == 0 ? [] :
            (await db.Terrains.Where(t => idsTerrains.Contains(t.Id)).Select(t => t.Id).ToListAsync(ct)).ToHashSet();
        var photos = idsPhotos.Count == 0 ? new Dictionary<int, int>() :
            await db.TerrainPhotos.Where(p => idsPhotos.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.TerrainId, ct);
        var documents = idsDocuments.Count == 0 ? new Dictionary<int, int>() :
            await db.DocumentsFonciers.Where(d => idsDocuments.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.TerrainId, ct);
        var panoramas = idsPanoramas.Count == 0 ? new Dictionary<int, int>() :
            await db.Panoramas.Where(p => idsPanoramas.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.TerrainId, ct);
        var photosServices = idsPhotosServices.Count == 0 ? new Dictionary<int, int>() :
            await db.ServicePhotos.Where(p => idsPhotosServices.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ServiceId, ct);
        var servicesExistants = idsServices.Count == 0 ? [] :
            (await db.Services.Where(s => idsServices.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct)).ToHashSet();

        foreach (var l in lignes)
        {
            var id = int.TryParse(l.EntiteId, out var n) ? n : 0;
            l.Lien = l.EntiteType switch
            {
                nameof(Terrain) when terrainsExistants.Contains(id) => $"/admin/terrains/{id}/infos",
                nameof(TerrainPhoto) when photos.TryGetValue(id, out var t) => $"/admin/terrains/{t}/photos",
                nameof(DocumentFoncier) when documents.TryGetValue(id, out var t) => $"/admin/terrains/{t}/documents",
                nameof(Panorama) when panoramas.TryGetValue(id, out var t) => $"/admin/terrains/{t}/visite-360",
                nameof(Service) when servicesExistants.Contains(id) => $"/admin/services/{id}",
                nameof(ServicePhoto) when photosServices.TryGetValue(id, out var s) => $"/admin/services/{s}",
                nameof(Localite) => "/admin/localites",
                nameof(ParametreSite) => "/admin/parametres",
                "Compte" when voirComptes => "/admin/utilisateurs",
                _ => null
            };
        }
    }
}
