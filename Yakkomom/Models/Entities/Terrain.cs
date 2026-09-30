using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>
/// Terrain mis en vente. Seul <see cref="Titre"/> est obligatoire : l'admin peut
/// enregistrer un brouillon et compléter la fiche plus tard.
/// </summary>
public class Terrain
{
    public int Id { get; set; }

    /// <summary>Référence lisible (YK-0001), générée par une séquence PostgreSQL.</summary>
    public string Reference { get; set; } = string.Empty;

    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TypeTerrain? Type { get; set; }
    public StatutTerrain Statut { get; set; } = StatutTerrain.Brouillon;

    // --- Prix et dimensions -------------------------------------------------
    /// <summary>Prix en FCFA (pas de centimes).</summary>
    public long? Prix { get; set; }
    public bool PrixNegociable { get; set; }
    public decimal? SurfaceM2 { get; set; }
    public decimal? LongueurM { get; set; }
    public decimal? LargeurM { get; set; }

    // --- Situation foncière -------------------------------------------------
    /// <summary>Type de document foncier déclaré (même si le fichier n'est pas encore téléversé).</summary>
    public TypeDocumentFoncier? SituationFonciere { get; set; }

    // --- Localisation -------------------------------------------------------
    public int? RegionId { get; set; }
    public Localite? Region { get; set; }
    public int? DepartementId { get; set; }
    public Localite? Departement { get; set; }
    public int? CommuneId { get; set; }
    public Localite? Commune { get; set; }
    /// <summary>Quartier ou village, en texte libre (ils sont trop nombreux pour une liste).</summary>
    public string? QuartierVillage { get; set; }
    public string? Adresse { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    /// <summary>Contour de la parcelle au format GeoJSON (Polygon), stocké en jsonb.</summary>
    public string? ContourGeoJson { get; set; }
    /// <summary>Surface calculée depuis le polygone, à titre indicatif.</summary>
    public decimal? SurfaceCalculeeM2 { get; set; }

    // --- Viabilisation (null = information inconnue) ------------------------
    public bool? AccesEau { get; set; }
    public bool? AccesElectricite { get; set; }
    public bool? RouteAcces { get; set; }
    public string? RouteAccesDetail { get; set; }
    public List<Commodite> Commodites { get; set; } = [];

    // --- Médias -------------------------------------------------------------
    public string? VideoYoutubeUrl { get; set; }

    // --- Contacts (optionnels, sinon ceux des paramètres du site) -----------
    public Contact Contacts { get; set; } = new();

    // --- Mise en avant ------------------------------------------------------
    public bool EstMisEnAvant { get; set; }
    public int OrdreMiseEnAvant { get; set; }

    // --- Traçabilité --------------------------------------------------------
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;
    /// <summary>Date du premier passage à un statut public.</summary>
    public DateTime? PublieLe { get; set; }
    public string? CreeParId { get; set; }
    public string? ModifieParId { get; set; }

    public List<TerrainPhoto> Photos { get; set; } = [];
    public List<Panorama> Panoramas { get; set; } = [];
    public List<DocumentFoncier> Documents { get; set; } = [];
    public List<ClicWhatsApp> ClicsWhatsApp { get; set; } = [];

    /// <summary>Visible dans l'espace public (tout sauf Brouillon et Archivé).</summary>
    public bool EstPublic => Statut is StatutTerrain.Disponible or StatutTerrain.Reserve or StatutTerrain.Vendu;
}

/// <summary>Distance à une commodité (école, marché, route nationale…), stockée en JSON dans le terrain.</summary>
public class Commodite
{
    public string Libelle { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
}
