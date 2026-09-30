using System.ComponentModel.DataAnnotations;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Services;

namespace Yakkomom.Models.ViewModels.Admin;

// ---------------------------------------------------------------------------
// Liste
// ---------------------------------------------------------------------------
public class FiltreTerrainsAdmin
{
    public string? Q { get; set; }
    public StatutTerrain? Statut { get; set; }
    /// <summary>Uniquement les fiches incomplètes.</summary>
    public bool Incomplets { get; set; }
    public int Page { get; set; } = 1;
}

public record PageResultat<T>(IReadOnlyList<T> Elements, int Total, int Page, int TaillePage)
{
    public int NombrePages => Math.Max(1, (int)Math.Ceiling(Total / (double)TaillePage));
}

public class TerrainLigneVm
{
    public int Id { get; init; }
    public required string Reference { get; init; }
    public required string Titre { get; init; }
    public StatutTerrain Statut { get; init; }
    public long? Prix { get; init; }
    public decimal? SurfaceM2 { get; init; }
    public string? Commune { get; init; }
    public DateTime ModifieLe { get; init; }
    public bool EstMisEnAvant { get; init; }
    public required ResultatCompletude Completude { get; init; }
}

public class ListeTerrainsAdminVm
{
    public required FiltreTerrainsAdmin Filtre { get; init; }
    public required PageResultat<TerrainLigneVm> Resultats { get; init; }
    public required IReadOnlyDictionary<StatutTerrain, int> CompteParStatut { get; init; }
}

// ---------------------------------------------------------------------------
// Création
// ---------------------------------------------------------------------------
public class NouveauTerrainVm
{
    [Required(ErrorMessage = "Donnez un titre au terrain.")]
    [StringLength(200, ErrorMessage = "200 caractères maximum.")]
    [Display(Name = "Titre du terrain")]
    public string Titre { get; set; } = string.Empty;
}

// ---------------------------------------------------------------------------
// Édition : en-tête commun + un modèle par onglet
// ---------------------------------------------------------------------------
public class EnTeteTerrainVm
{
    public int Id { get; init; }
    public required string Reference { get; init; }
    public required string Titre { get; init; }
    public StatutTerrain Statut { get; init; }
    public DateTime ModifieLe { get; init; }
    public required string UrlPublique { get; init; }
    public required ResultatCompletude Completude { get; init; }
    public OngletTerrain OngletActif { get; set; }
}

public abstract class OngletTerrainVm
{
    /// <summary>Renseigné par le contrôleur pour l'affichage.</summary>
    public EnTeteTerrainVm? EnTete { get; set; }
    /// <summary>« continuer » : enregistrer puis passer à l'onglet suivant.</summary>
    public string? Suite { get; set; }
}

public class InfosTerrainVm : OngletTerrainVm
{
    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200, ErrorMessage = "200 caractères maximum.")]
    [Display(Name = "Titre")]
    public string Titre { get; set; } = string.Empty;

    [Display(Name = "Type de terrain")]
    public TypeTerrain? Type { get; set; }

    [StringLength(5000, ErrorMessage = "5000 caractères maximum.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Prix (FCFA)")]
    public string? Prix { get; set; }

    [Display(Name = "Prix négociable")]
    public bool PrixNegociable { get; set; }

    [Display(Name = "Surface (m²)")]
    public string? SurfaceM2 { get; set; }

    [Display(Name = "Longueur (m)")]
    public string? LongueurM { get; set; }

    [Display(Name = "Largeur (m)")]
    public string? LargeurM { get; set; }

    [Display(Name = "Eau")]
    public bool? AccesEau { get; set; }

    [Display(Name = "Électricité")]
    public bool? AccesElectricite { get; set; }

    [Display(Name = "Route d'accès")]
    public bool? RouteAcces { get; set; }

    [StringLength(200)]
    [Display(Name = "Précision sur l'accès")]
    public string? RouteAccesDetail { get; set; }

    [Display(Name = "Mettre en avant sur l'accueil")]
    public bool EstMisEnAvant { get; set; }

    /// <summary>Surface calculée depuis le contour dessiné (affichage uniquement).</summary>
    public decimal? SurfaceCalculeeM2 { get; set; }
}

public class CommoditeSaisie
{
    [StringLength(100)]
    public string? Libelle { get; set; }
    public string? DistanceKm { get; set; }
}

public class LocalisationTerrainVm : OngletTerrainVm
{
    [Display(Name = "Région")]
    public int? RegionId { get; set; }

    [Display(Name = "Département")]
    public int? DepartementId { get; set; }

    [Display(Name = "Commune")]
    public int? CommuneId { get; set; }

    [StringLength(120)]
    [Display(Name = "Quartier ou village")]
    public string? QuartierVillage { get; set; }

    [StringLength(300)]
    [Display(Name = "Adresse ou repère")]
    public string? Adresse { get; set; }

    [Display(Name = "Latitude")]
    public string? Latitude { get; set; }

    [Display(Name = "Longitude")]
    public string? Longitude { get; set; }

    /// <summary>Contour de la parcelle (GeoJSON Polygon), dessiné sur la carte.</summary>
    public string? ContourGeoJson { get; set; }

    /// <summary>Surface calculée depuis le contour (affichage uniquement).</summary>
    public decimal? SurfaceCalculeeM2 { get; set; }

    public List<CommoditeSaisie> Commodites { get; set; } = [];

    /// <summary>Listes pour les menus déroulants.</summary>
    public IReadOnlyList<LocaliteOption> Localites { get; set; } = [];
}

public record LocaliteOption(int Id, string Nom, TypeLocalite Type, int? ParentId, bool EstActive);

public class MediasTerrainVm : OngletTerrainVm
{
    [StringLength(300)]
    [Display(Name = "Lien de la vidéo YouTube")]
    public string? VideoYoutubeUrl { get; set; }

    public IReadOnlyList<PanoramaAdminVm> Panoramas { get; set; } = [];
    /// <summary>Configuration Pannellum de l'éditeur (versions légères).</summary>
    public string? ConfigEdition { get; set; }
}

public class DocumentsTerrainVm : OngletTerrainVm
{
    [Display(Name = "Situation foncière déclarée")]
    public TypeDocumentFoncier? SituationFonciere { get; set; }

    public IReadOnlyList<DocumentAdminVm> Documents { get; set; } = [];
}

/// <summary>Champs de contact communs (terrain et paramètres par défaut).</summary>
public interface IChampsContacts
{
    string? WhatsApp1 { get; }
    string? WhatsApp1Libelle { get; }
    string? WhatsApp2 { get; }
    string? WhatsApp2Libelle { get; }
    string? WhatsApp3 { get; }
    string? WhatsApp3Libelle { get; }
    string? Email { get; }
}

public class ContactsTerrainVm : OngletTerrainVm, IChampsContacts
{
    [Display(Name = "WhatsApp 1")] public string? WhatsApp1 { get; set; }
    [StringLength(60)][Display(Name = "Nom affiché")] public string? WhatsApp1Libelle { get; set; }
    [Display(Name = "WhatsApp 2")] public string? WhatsApp2 { get; set; }
    [StringLength(60)][Display(Name = "Nom affiché")] public string? WhatsApp2Libelle { get; set; }
    [Display(Name = "WhatsApp 3")] public string? WhatsApp3 { get; set; }
    [StringLength(60)][Display(Name = "Nom affiché")] public string? WhatsApp3Libelle { get; set; }

    // Validé (avec les numéros) par ContactsSaisie, pour afficher toutes les erreurs en une fois.
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    /// <summary>Contacts par défaut du site, affichés pour information.</summary>
    public Contact? ContactsParDefaut { get; set; }
}

public class ChangementStatutVm
{
    public StatutTerrain Statut { get; set; }
}
