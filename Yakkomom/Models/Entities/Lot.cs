using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>
/// Lot d'un lotissement (grand terrain découpé). Chaque lot a son prix selon sa position :
/// un prix au m² (cas courant) ou un prix fixe, prioritaire s'il est renseigné.
/// </summary>
public class Lot
{
    public int Id { get; set; }
    public int TerrainId { get; set; }
    public Terrain Terrain { get; set; } = null!;

    /// <summary>Numéro tel qu'il figure sur le plan : « 12 », « A-12 »…</summary>
    public string Numero { get; set; } = string.Empty;
    public decimal? SurfaceM2 { get; set; }
    /// <summary>Prix au m² en FCFA.</summary>
    public long? PrixM2 { get; set; }
    /// <summary>Prix fixe du lot en FCFA ; s'il est renseigné, il remplace le calcul surface × prix au m².</summary>
    public long? Prix { get; set; }
    /// <summary>Ce qui justifie le prix : « Angle », « Bord de route », « Face mer »…</summary>
    public string? Position { get; set; }
    public StatutLot Statut { get; set; } = StatutLot.Disponible;
    public int Ordre { get; set; }
    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;

    /// <summary>Prix du lot : fixe, sinon surface × prix au m².</summary>
    public long? PrixEffectif => Prix ?? (PrixM2 is > 0 && SurfaceM2 is > 0 ? (long)Math.Round(PrixM2.Value * SurfaceM2.Value) : null);

    /// <summary>Prix au m² : saisi, sinon déduit du prix fixe.</summary>
    public long? PrixM2Effectif => PrixM2 ?? (Prix is > 0 && SurfaceM2 is > 0 ? (long)Math.Round(Prix.Value / SurfaceM2.Value) : null);
}
