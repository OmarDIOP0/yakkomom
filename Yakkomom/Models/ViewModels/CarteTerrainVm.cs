using Yakkomom.Models.Enums;

namespace Yakkomom.Models.ViewModels;

/// <summary>Données minimales d'une carte terrain (liste, accueil, favoris).</summary>
public class CarteTerrainVm
{
    public required string Reference { get; init; }
    public required string Titre { get; init; }
    public required string Url { get; init; }
    public string? Localisation { get; init; }
    public long? Prix { get; init; }
    public decimal? SurfaceM2 { get; init; }
    public StatutTerrain Statut { get; init; }

    public string? ImageUrl { get; init; }
    /// <summary>Variantes pour srcset, ex. « …-480.webp 480w, …-960.webp 960w ».</summary>
    public string? ImageSrcset { get; init; }
    public int ImageLargeur { get; init; } = 800;
    public int ImageHauteur { get; init; } = 600;
    public string? CouleurDominante { get; init; }

    /// <summary>Lotissement découpé : nombre de lots disponibles, fourchettes (sinon <see cref="ResumeLots.Aucun"/>).</summary>
    public Services.ResumeLots Lots { get; init; } = Services.ResumeLots.Aucun;

    public bool TitreFoncier { get; init; }
    public bool Visite360 { get; init; }
    /// <summary>La première carte visible ne doit pas être en lazy-loading (LCP).</summary>
    public bool Prioritaire { get; init; }
}
