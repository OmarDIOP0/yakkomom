using Yakkomom.Models.ViewModels;

namespace Yakkomom.Services.Interfaces;

/// <summary>Lecture des terrains publiés (Disponible, Réservé, Vendu) pour l'espace public.</summary>
public interface ITerrainPublicService
{
    Task<ListeTerrainsPublicVm> ListerAsync(FiltreTerrainsPublic filtre, CancellationToken ct = default);

    /// <summary>Fiche complète ; null si inconnue ou non publiée (sauf aperçu admin).</summary>
    Task<FicheTerrainVm?> FicheAsync(string reference, bool apercuAdmin, CancellationToken ct = default);

    Task<AccueilVm> AccueilAsync(CancellationToken ct = default);

    /// <summary>Données à jour des cartes (favoris), dans l'ordre demandé.</summary>
    Task<IReadOnlyList<CarteTerrainVm>> CartesAsync(IReadOnlyList<string> references, CancellationToken ct = default);
}
