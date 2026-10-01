using System.ComponentModel.DataAnnotations;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Services;

namespace Yakkomom.Models.ViewModels.Admin;

public class LotsAdminVm
{
    public EnTeteTerrainVm? EnTete { get; set; }
    public int TerrainId { get; init; }
    public required string Reference { get; init; }
    public StatutTerrain StatutTerrain { get; init; }
    public required FourchetteVm Fourchette { get; init; }
    public required IReadOnlyList<Lot> Lots { get; init; }
    public required ResumeLots Resume { get; init; }
    public SerieLotsVm Serie { get; init; } = new();
}

/// <summary>Fourchette de prix au m² annoncée avant que les lots soient chiffrés.</summary>
public class FourchetteVm
{
    [Display(Name = "Prix au m² minimum (FCFA)")] public string? PrixM2Min { get; set; }
    [Display(Name = "Prix au m² maximum (FCFA)")] public string? PrixM2Max { get; set; }
}

/// <summary>Création rapide : « lots 1 à 40, 300 m², 15 000 F/m² ».</summary>
public class SerieLotsVm
{
    [Display(Name = "Préfixe (facultatif)")] public string? Prefixe { get; set; }
    [Display(Name = "Du lot n°")] public int? Debut { get; set; } = 1;
    [Display(Name = "au lot n°")] public int? Fin { get; set; }
    [Display(Name = "Surface de chaque lot (m²)")] public string? SurfaceM2 { get; set; }
    [Display(Name = "Prix au m² (FCFA)")] public string? PrixM2 { get; set; }
}

public class LotSaisieVm
{
    public string? Numero { get; set; }
    public string? SurfaceM2 { get; set; }
    public string? PrixM2 { get; set; }
    public string? Prix { get; set; }
    public string? Position { get; set; }
    public StatutLot Statut { get; set; }
}
