using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;

namespace Yakkomom.Services;

/// <summary>
/// Chiffres clés d'un lotissement, calculés à partir de ses lots.
/// Les fourchettes portent sur les lots encore disponibles ; s'il n'y en a plus (ou s'ils ne sont pas chiffrés),
/// sur tous les lots, puis sur la fourchette saisie à la main.
/// </summary>
public record ResumeLots(
    int Total, int Disponibles, int Reserves, int Vendus,
    long? PrixMin, long? PrixMax, long? PrixM2Min, long? PrixM2Max, decimal? SurfaceMin, decimal? SurfaceMax)
{
    public static readonly ResumeLots Aucun = new(0, 0, 0, 0, null, null, null, null, null, null);

    public bool EstLotissement => Total > 0;

    public static ResumeLots Calculer(IReadOnlyCollection<Lot> lots, long? prixM2MinSaisi = null, long? prixM2MaxSaisi = null)
    {
        var dispo = lots.Where(l => l.Statut == StatutLot.Disponible).ToList();
        // Base des fourchettes : lots disponibles chiffrés, sinon tous les lots chiffrés
        var base_ = dispo.Any(l => l.PrixEffectif is not null) ? dispo : lots.ToList();

        var prix = base_.Select(l => l.PrixEffectif).OfType<long>().ToList();
        var prixM2 = base_.Select(l => l.PrixM2Effectif).OfType<long>().ToList();
        var surfaces = (dispo.Count > 0 ? dispo : lots).Select(l => l.SurfaceM2).OfType<decimal>().ToList();

        return new ResumeLots(
            lots.Count, dispo.Count,
            lots.Count(l => l.Statut == StatutLot.Reserve), lots.Count(l => l.Statut == StatutLot.Vendu),
            prix.Count > 0 ? prix.Min() : null, prix.Count > 0 ? prix.Max() : null,
            prixM2.Count > 0 ? prixM2.Min() : prixM2MinSaisi, prixM2.Count > 0 ? prixM2.Max() : prixM2MaxSaisi,
            surfaces.Count > 0 ? surfaces.Min() : null, surfaces.Count > 0 ? surfaces.Max() : null);
    }

    /// <summary>
    /// Prix « à partir de » enregistré sur le terrain parent : il sert aux filtres, au tri et aux aperçus de partage.
    /// Lot disponible le moins cher, sinon lot le moins cher, sinon rien.
    /// </summary>
    public static long? PrixParent(IReadOnlyCollection<Lot> lots) =>
        lots.Where(l => l.Statut == StatutLot.Disponible).Select(l => l.PrixEffectif).OfType<long>().DefaultIfEmpty().Min() is var m and > 0
            ? m
            : lots.Select(l => l.PrixEffectif).OfType<long>().DefaultIfEmpty().Min() is var t and > 0 ? t : null;
}
