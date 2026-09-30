using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

public class LocaliteService(YakkomomDbContext db, IMemoryCache cache, IJournalService journal) : ILocaliteService
{
    private const string CleCache = "localites";

    public async Task<IReadOnlyList<LocaliteOption>> ListerAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CleCache, out IReadOnlyList<LocaliteOption>? enCache) && enCache is not null) return enCache;

        var liste = await db.Localites.AsNoTracking()
            .OrderBy(l => l.Type).ThenBy(l => l.Ordre).ThenBy(l => l.Nom)
            .Select(l => new LocaliteOption(l.Id, l.Nom, l.Type, l.ParentId, l.EstActive))
            .ToListAsync(ct);
        cache.Set(CleCache, (IReadOnlyList<LocaliteOption>)liste, TimeSpan.FromHours(1));
        return liste;
    }

    public async Task<(int? RegionId, int? DepartementId, int? CommuneId, string? Erreur)> ResoudreAsync(
        int? regionId, int? departementId, int? communeId, CancellationToken ct = default)
    {
        var index = (await ListerAsync(ct)).ToDictionary(l => l.Id);
        LocaliteOption? Trouver(int? id, TypeLocalite type) =>
            id is { } v && index.TryGetValue(v, out var l) && l.Type == type ? l : null;

        var commune = Trouver(communeId, TypeLocalite.Commune);
        var departement = Trouver(departementId, TypeLocalite.Departement);
        var region = Trouver(regionId, TypeLocalite.Region);

        if (communeId is not null && commune is null) return (null, null, null, "Commune inconnue.");
        if (departementId is not null && departement is null) return (null, null, null, "Département inconnu.");
        if (regionId is not null && region is null) return (null, null, null, "Région inconnue.");

        // Le niveau le plus précis fait foi : on remonte la hiérarchie.
        if (commune is not null)
        {
            if (departement is not null && commune.ParentId != departement.Id)
                return (null, null, null, "Cette commune n'appartient pas au département choisi.");
            departement = index[commune.ParentId!.Value];
        }
        if (departement is not null)
        {
            if (region is not null && departement.ParentId != region.Id)
                return (null, null, null, "Ce département n'appartient pas à la région choisie.");
            region = index[departement.ParentId!.Value];
        }
        return (region?.Id, departement?.Id, commune?.Id, null);
    }

    public async Task<ResultatOperation> AjouterAsync(string nom, TypeLocalite type, int? parentId, CancellationToken ct = default)
    {
        nom = nom.Trim();
        if (nom.Length is < 2 or > 120) return ResultatOperation.Echec("Le nom doit faire entre 2 et 120 caractères.");

        var typeParentAttendu = type switch
        {
            TypeLocalite.Departement => TypeLocalite.Region,
            TypeLocalite.Commune => TypeLocalite.Departement,
            _ => (TypeLocalite?)null
        };
        if (typeParentAttendu is not null)
        {
            var parent = await db.Localites.AsNoTracking().FirstOrDefaultAsync(l => l.Id == parentId, ct);
            if (parent is null || parent.Type != typeParentAttendu) return ResultatOperation.Echec("Niveau parent invalide.");
        }
        else parentId = null;

        var slug = SlugHelper.Slugifier(nom);
        if (await db.Localites.AnyAsync(l => l.Type == type && l.ParentId == parentId && l.Slug == slug, ct))
            return ResultatOperation.Echec($"« {nom} » existe déjà à cet endroit.");

        var ordre = await db.Localites.Where(l => l.Type == type && l.ParentId == parentId).MaxAsync(l => (int?)l.Ordre, ct) ?? -1;
        var localite = new Localite { Nom = nom, Slug = slug, Type = type, ParentId = parentId, Ordre = ordre + 1 };
        db.Localites.Add(localite);
        await db.SaveChangesAsync(ct);
        cache.Remove(CleCache);
        await journal.EnregistrerAsync(TypeAction.Creation, nameof(Localite), localite.Id.ToString(), $"Ajout de la localité {nom}", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> RenommerAsync(int id, string nom, CancellationToken ct = default)
    {
        nom = nom.Trim();
        if (nom.Length is < 2 or > 120) return ResultatOperation.Echec("Le nom doit faire entre 2 et 120 caractères.");
        var localite = await db.Localites.FindAsync([id], ct);
        if (localite is null) return ResultatOperation.Echec("Localité introuvable.");

        var slug = SlugHelper.Slugifier(nom);
        if (await db.Localites.AnyAsync(l => l.Id != id && l.Type == localite.Type && l.ParentId == localite.ParentId && l.Slug == slug, ct))
            return ResultatOperation.Echec($"« {nom} » existe déjà à cet endroit.");

        var ancien = localite.Nom;
        localite.Nom = nom;
        localite.Slug = slug;
        await db.SaveChangesAsync(ct);
        cache.Remove(CleCache);
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(Localite), id.ToString(), $"Localité « {ancien} » renommée en « {nom} »", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> BasculerActiveAsync(int id, CancellationToken ct = default)
    {
        var localite = await db.Localites.FindAsync([id], ct);
        if (localite is null) return ResultatOperation.Echec("Localité introuvable.");
        localite.EstActive = !localite.EstActive;
        await db.SaveChangesAsync(ct);
        cache.Remove(CleCache);
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(Localite), id.ToString(),
            $"Localité {localite.Nom} {(localite.EstActive ? "réactivée" : "masquée")}", ct: ct);
        return ResultatOperation.Ok();
    }
}
