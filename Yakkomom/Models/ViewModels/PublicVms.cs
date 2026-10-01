using Yakkomom.Models.Enums;

namespace Yakkomom.Models.ViewModels;

/// <summary>Photo prête à afficher : src, srcset et dimensions (évite les sauts de mise en page).</summary>
public record PhotoPubliqueVm(string Src, string? Srcset, string UrlGrande, int Largeur, int Hauteur, string? Couleur, string? Legende);

/// <summary>Bouton WhatsApp : lien de suivi interne (/wa/…) qui redirige vers wa.me.</summary>
public record ContactWhatsAppVm(int Index, string NumeroAffiche, string? Libelle, string UrlSuivi);

public record DocumentPublicVm(int Id, string Type, string? Titre, string Url);

public record CommoditeVm(string Libelle, decimal? DistanceKm, int? DureeMinutes = null, ModeTrajet? Mode = null)
{
    /// <summary>« 1,5 km · 5 min en voiture », « 300 m · 4 min à pied ».</summary>
    public string Texte => string.Join(" · ", new[]
    {
        DistanceKm is { } d ? (d < 1 ? $"{Math.Round(d * 1000):0} m" : $"{Helpers.Format.Nombre(d, d % 1 == 0 ? 0 : 1)} km") : null,
        DureeMinutes is { } m ? $"{m} min{(Mode == ModeTrajet.APied ? " à pied" : Mode == ModeTrajet.Voiture ? " en voiture" : "")}" : null
    }.Where(x => x is not null));
}

public class FiltreTerrainsPublic
{
    public string? Q { get; set; }
    public int? Region { get; set; }
    public int? Commune { get; set; }
    public TypeTerrain? Type { get; set; }
    /// <summary>« disponible » : uniquement les terrains disponibles ; sinon tous les statuts publics.</summary>
    public string? Statut { get; set; }
    public string? PrixMin { get; set; }
    public string? PrixMax { get; set; }
    public string? SurfaceMin { get; set; }
    public string? SurfaceMax { get; set; }
    public bool Visite360 { get; set; }
    public bool TitreFoncier { get; set; }
    /// <summary>recent (défaut), prix, prix-desc, surface, surface-desc</summary>
    public string? Tri { get; set; }
    public int Page { get; set; } = 1;

    public int NombreFiltresActifs =>
        new object?[] { Region, Commune, Type, Statut, PrixMin, PrixMax, SurfaceMin, SurfaceMax }
            .Count(v => v is not null && v is not string { Length: 0 }) + (Visite360 ? 1 : 0) + (TitreFoncier ? 1 : 0);
}

public record OptionLieu(int Id, string Nom, int? ParentId, int Nombre);

public class ListeTerrainsPublicVm
{
    public required FiltreTerrainsPublic Filtre { get; init; }
    public required IReadOnlyList<CarteTerrainVm> Terrains { get; init; }
    public int Total { get; init; }
    public int Page { get; init; }
    public int NombrePages { get; init; }
    /// <summary>Régions et communes ayant au moins un terrain publié.</summary>
    public IReadOnlyList<OptionLieu> Regions { get; init; } = [];
    public IReadOnlyList<OptionLieu> Communes { get; init; } = [];
    public IReadOnlyDictionary<int, string> NomsDepartements { get; init; } = new Dictionary<int, string>();
}

public class FicheTerrainVm
{
    public required string Reference { get; init; }
    public required string Titre { get; init; }
    public string? Description { get; init; }
    public TypeTerrain? Type { get; init; }
    public StatutTerrain Statut { get; init; }
    public long? Prix { get; init; }
    public bool PrixNegociable { get; init; }
    public decimal? SurfaceM2 { get; init; }
    public decimal? SurfaceCalculeeM2 { get; init; }
    public decimal? LongueurM { get; init; }
    public decimal? LargeurM { get; init; }
    public TypeDocumentFoncier? SituationFonciere { get; init; }

    public string? Region { get; init; }
    public string? Departement { get; init; }
    public string? Commune { get; init; }
    public string? QuartierVillage { get; init; }
    public string? Adresse { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? ContourGeoJson { get; init; }

    public bool? AccesEau { get; init; }
    public bool? AccesElectricite { get; init; }
    public bool? RouteAcces { get; init; }
    public string? RouteAccesDetail { get; init; }
    public IReadOnlyList<CommoditeVm> Commodites { get; init; } = [];

    public IReadOnlyList<PhotoPubliqueVm> Photos { get; init; } = [];
    public int NombrePanoramas { get; init; }
    public VisitePubliqueVm? Visite { get; init; }
    public string? VideoId { get; init; }
    public IReadOnlyList<DocumentPublicVm> DocumentsPublics { get; init; } = [];
    public bool AUnTitreFoncier { get; init; }

    // Lotissement
    public IReadOnlyList<LotPublicVm> Lots { get; init; } = [];
    public Services.ResumeLots ResumeLots { get; init; } = Services.ResumeLots.Aucun;
    /// <summary>Plan de lotissement public : affiché dans la page si c'est une image, lien sinon.</summary>
    public DocumentPublicVm? PlanLotissement { get; init; }
    public bool PlanEstImage { get; init; }

    public IReadOnlyList<ContactWhatsAppVm> WhatsApp { get; init; } = [];
    public string? UrlDemandeDocument { get; init; }
    public string? Email { get; init; }

    public required string UrlCanonique { get; init; }
    public required string Resume { get; init; }
    public IReadOnlyList<CarteTerrainVm> Similaires { get; init; } = [];
    public DateTime ModifieLe { get; init; }
    public DateTime? PublieLe { get; init; }
    /// <summary>Aperçu de partage (WhatsApp, Facebook) : photo de couverture ou image par défaut.</summary>
    public ImagePartage? ImagePartage { get; init; }
    /// <summary>Terrain non publié affiché à un admin connecté (bandeau « Aperçu »).</summary>
    public bool EstApercuAdmin { get; init; }

    public string Localisation => string.Join(", ", new[] { QuartierVillage, Commune, Departement, Region }
        .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
}

/// <summary>Lot affiché sur la fiche d'un lotissement, avec son lien WhatsApp (message « lot 12 »).</summary>
public record LotPublicVm(string Numero, decimal? SurfaceM2, long? PrixM2, long? Prix, string? Position, StatutLot Statut, string? UrlWhatsApp);

public record ServiceResumeVm(string Titre, string Slug, string? Resume, string? Icone);

public class AccueilVm
{
    public IReadOnlyList<CarteTerrainVm> ALaUne { get; init; } = [];
    public IReadOnlyList<ServiceResumeVm> Services { get; init; } = [];
    public int NombreDisponibles { get; init; }
    public int NombreVendus { get; init; }
    public ContactWhatsAppVm? WhatsAppPrincipal { get; init; }
}
