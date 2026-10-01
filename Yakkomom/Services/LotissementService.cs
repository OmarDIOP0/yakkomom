using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

/// <summary>Découpage d'un grand terrain en lots (admin).</summary>
public partial class LotissementService(YakkomomDbContext db, IJournalService journal, IHttpContextAccessor http) : ILotissementService
{
    public const int LotsMax = 1000;

    public async Task<LotsAdminVm?> ListerAsync(int terrainId, CancellationToken ct = default)
    {
        var t = await db.Terrains.AsNoTracking().Where(x => x.Id == terrainId)
            .Select(x => new { x.Id, x.Reference, x.PrixM2Min, x.PrixM2Max, x.Statut }).FirstOrDefaultAsync(ct);
        if (t is null) return null;
        var lots = await db.Lots.AsNoTracking().Where(l => l.TerrainId == terrainId).ToListAsync(ct);
        lots = Trier(lots).ToList();
        return new LotsAdminVm
        {
            TerrainId = t.Id, Reference = t.Reference, StatutTerrain = t.Statut,
            Fourchette = new FourchetteVm { PrixM2Min = Nombres.PourSaisie(t.PrixM2Min), PrixM2Max = Nombres.PourSaisie(t.PrixM2Max) },
            Lots = lots,
            Resume = ResumeLots.Calculer(lots, t.PrixM2Min, t.PrixM2Max)
        };
    }

    public async Task<ResultatOperation> EnregistrerFourchetteAsync(int terrainId, FourchetteVm vm, CancellationToken ct = default)
    {
        var r = ResultatOperation.Ok();
        var min = LireMontant(vm.PrixM2Min, nameof(vm.PrixM2Min), r);
        var max = LireMontant(vm.PrixM2Max, nameof(vm.PrixM2Max), r);
        if (min is not null && max is not null && min > max) r.AjouterErreur(nameof(vm.PrixM2Max), "Le maximum doit être supérieur au minimum.");
        if (!r.Reussi) return r;

        var t = await db.Terrains.FirstOrDefaultAsync(x => x.Id == terrainId, ct);
        if (t is null) return ResultatOperation.Echec("Terrain introuvable.");
        t.PrixM2Min = min;
        t.PrixM2Max = max ?? min;
        t.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(Terrain), terrainId.ToString(),
            $"{t.Reference} : fourchette de prix au m² {Format.Fcfa(min)} – {Format.Fcfa(t.PrixM2Max)}", ct: ct);
        return r;
    }

    public async Task<ResultatOperation> CreerSerieAsync(int terrainId, SerieLotsVm vm, CancellationToken ct = default)
    {
        var r = ResultatOperation.Ok();
        if (vm.Debut is null or < 0 || vm.Fin is null || vm.Fin < vm.Debut)
            r.AjouterErreur(nameof(vm.Fin), "Indiquez une plage de numéros valide (ex. de 1 à 40).");
        else if (vm.Fin - vm.Debut + 1 > 500)
            r.AjouterErreur(nameof(vm.Fin), "500 lots au plus par série.");
        var surface = LireSurface(vm.SurfaceM2, nameof(vm.SurfaceM2), r);
        var prixM2 = LireMontant(vm.PrixM2, nameof(vm.PrixM2), r);
        var prefixe = Nettoyer(vm.Prefixe, 10);
        if (!r.Reussi) return r;

        var t = await db.Terrains.FirstOrDefaultAsync(x => x.Id == terrainId, ct);
        if (t is null) return ResultatOperation.Echec("Terrain introuvable.");
        var existants = (await db.Lots.Where(l => l.TerrainId == terrainId).Select(l => l.Numero).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ordre = await db.Lots.Where(l => l.TerrainId == terrainId).MaxAsync(l => (int?)l.Ordre, ct) ?? 0;

        var crees = 0;
        for (var n = vm.Debut!.Value; n <= vm.Fin!.Value; n++)
        {
            var numero = $"{prefixe}{n}";
            if (!existants.Add(numero)) continue; // déjà présent : on ne l'écrase pas
            db.Lots.Add(new Lot { TerrainId = terrainId, Numero = numero, SurfaceM2 = surface, PrixM2 = prixM2, Ordre = ++ordre });
            crees++;
        }
        if (existants.Count > LotsMax) return ResultatOperation.Echec($"{LotsMax} lots au plus par lotissement.");
        if (crees == 0) return ResultatOperation.Echec("Tous ces numéros de lots existent déjà.");

        await db.SaveChangesAsync(ct);
        await SynchroniserParentAsync(t, ct);
        await journal.EnregistrerAsync(TypeAction.Creation, nameof(Lot), terrainId.ToString(),
            $"{t.Reference} : {crees} lot{(crees > 1 ? "s" : "")} ajouté{(crees > 1 ? "s" : "")} ({prefixe}{vm.Debut} à {prefixe}{vm.Fin})", ct: ct);
        r.Message = $"{crees} lot{(crees > 1 ? "s" : "")} ajouté{(crees > 1 ? "s" : "")}.";
        return r;
    }

    public async Task<ResultatOperation> EnregistrerLotAsync(int terrainId, int lotId, LotSaisieVm vm, CancellationToken ct = default)
    {
        var r = ResultatOperation.Ok();
        var numero = Nettoyer(vm.Numero, 20);
        if (numero is null) r.AjouterErreur(nameof(vm.Numero), "Le numéro du lot est obligatoire.");
        var surface = LireSurface(vm.SurfaceM2, nameof(vm.SurfaceM2), r);
        var prixM2 = LireMontant(vm.PrixM2, nameof(vm.PrixM2), r);
        var prix = LireMontant(vm.Prix, nameof(vm.Prix), r);
        if (!Enum.IsDefined(vm.Statut)) r.AjouterErreur(nameof(vm.Statut), "Statut inconnu.");
        if (!r.Reussi) return r;

        var lot = await db.Lots.Include(l => l.Terrain).FirstOrDefaultAsync(l => l.Id == lotId && l.TerrainId == terrainId, ct);
        if (lot is null) return ResultatOperation.Echec("Lot introuvable.");
        if (!string.Equals(lot.Numero, numero, StringComparison.OrdinalIgnoreCase) &&
            await db.Lots.AnyAsync(l => l.TerrainId == terrainId && l.Id != lotId && l.Numero == numero, ct))
            return ResultatOperation.Echec($"Le lot {numero} existe déjà.");

        var ancienStatut = lot.Statut;
        lot.Numero = numero!;
        lot.SurfaceM2 = surface;
        lot.PrixM2 = prixM2;
        lot.Prix = prix;
        lot.Position = Nettoyer(vm.Position, 120);
        lot.Statut = vm.Statut;
        lot.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await SynchroniserParentAsync(lot.Terrain, ct);

        await journal.EnregistrerAsync(ancienStatut != lot.Statut ? TypeAction.ChangementStatut : TypeAction.Modification, nameof(Lot), lot.Id.ToString(),
            ancienStatut != lot.Statut
                ? $"{lot.Terrain.Reference} : lot {lot.Numero} {Libelle(ancienStatut)} → {Libelle(lot.Statut)}"
                : $"{lot.Terrain.Reference} : modification du lot {lot.Numero}", ct: ct);
        return r;
    }

    public async Task<ResultatOperation> ChangerStatutAsync(int terrainId, int lotId, StatutLot statut, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(statut)) return ResultatOperation.Echec("Statut inconnu.");
        var lot = await db.Lots.Include(l => l.Terrain).FirstOrDefaultAsync(l => l.Id == lotId && l.TerrainId == terrainId, ct);
        if (lot is null) return ResultatOperation.Echec("Lot introuvable.");
        if (lot.Statut == statut) return ResultatOperation.Ok();
        var ancien = lot.Statut;
        lot.Statut = statut;
        lot.ModifieLe = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await SynchroniserParentAsync(lot.Terrain, ct);
        await journal.EnregistrerAsync(TypeAction.ChangementStatut, nameof(Lot), lot.Id.ToString(),
            $"{lot.Terrain.Reference} : lot {lot.Numero} {Libelle(ancien)} → {Libelle(statut)}", ct: ct);
        return ResultatOperation.Ok();
    }

    public async Task<ResultatOperation> SupprimerAsync(int terrainId, int lotId, CancellationToken ct = default)
    {
        var lot = await db.Lots.Include(l => l.Terrain).FirstOrDefaultAsync(l => l.Id == lotId && l.TerrainId == terrainId, ct);
        if (lot is null) return ResultatOperation.Echec("Lot introuvable.");
        db.Lots.Remove(lot);
        await db.SaveChangesAsync(ct);
        await SynchroniserParentAsync(lot.Terrain, ct);
        await journal.EnregistrerAsync(TypeAction.Suppression, nameof(Lot), lot.Id.ToString(),
            $"{lot.Terrain.Reference} : suppression du lot {lot.Numero}", ct: ct);
        return ResultatOperation.Ok();
    }

    /// <summary>Le terrain parent affiche « à partir de » : son prix suit le lot disponible le moins cher.</summary>
    private async Task SynchroniserParentAsync(Terrain terrain, CancellationToken ct)
    {
        var lots = await db.Lots.AsNoTracking().Where(l => l.TerrainId == terrain.Id).ToListAsync(ct);
        terrain.Prix = lots.Count > 0 ? ResumeLots.PrixParent(lots) : terrain.Prix;
        terrain.ModifieLe = DateTime.UtcNow;
        terrain.ModifieParId = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Ordre naturel des numéros : 2 avant 10, « A-2 » avant « A-10 ».</summary>
    public static IEnumerable<Lot> Trier(IEnumerable<Lot> lots) => lots
        .OrderBy(l => ChiffresRegex().Replace(l.Numero, m => m.Value.PadLeft(8, '0')), StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"\d+")]
    private static partial Regex ChiffresRegex();

    public static string Libelle(StatutLot s) => s switch
    {
        StatutLot.Reserve => "Réservé",
        StatutLot.Vendu => "Vendu",
        _ => "Disponible"
    };

    private static long? LireMontant(string? saisie, string champ, ResultatOperation r)
    {
        if (!Nombres.LireMontant(saisie, out var v) || v is < 0 or > 100_000_000_000) { r.AjouterErreur(champ, "Montant invalide."); return null; }
        return v is 0 ? null : v;
    }

    private static decimal? LireSurface(string? saisie, string champ, ResultatOperation r)
    {
        if (!Nombres.LireDecimal(saisie, out var v) || v is <= 0 or > 100_000_000m) { r.AjouterErreur(champ, "Surface invalide."); return null; }
        return v;
    }

    private static string? Nettoyer(string? s, int max) =>
        string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > max ? s.Trim()[..max] : s.Trim());
}
