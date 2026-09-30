using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>Photo panoramique équirectangulaire (visite 360° avec Pannellum).</summary>
public class Panorama
{
    public int Id { get; set; }
    public int TerrainId { get; set; }
    public Terrain Terrain { get; set; } = null!;

    /// <summary>Ex. « Entrée du terrain », « Vue de la route ».</summary>
    public string Titre { get; set; } = string.Empty;

    /// <summary>Version haute résolution (plein écran / Wi-Fi).</summary>
    public string CleStockageHd { get; set; } = string.Empty;
    public long TailleOctetsHd { get; set; }
    /// <summary>Version basse résolution chargée par défaut (économie de données).</summary>
    public string? CleStockageBd { get; set; }
    public long TailleOctetsBd { get; set; }
    /// <summary>Vignette légère affichée avant le lancement de la visite.</summary>
    public string? CleVignette { get; set; }

    /// <summary>Dimensions de la version HD (ratio 2:1 pour une photo sphère complète).</summary>
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    /// <summary>
    /// Angle de vue vertical couvert (degrés) : 180 pour une photo sphère, moins pour un
    /// « mode panorama » de téléphone qui fait le tour horizontal sans couvrir le ciel et le sol.
    /// </summary>
    public double Vaov { get; set; } = 180;

    /// <summary>Identifiant d'envoi généré par le navigateur (anti-doublon en cas de reprise).</summary>
    public Guid? IdEnvoi { get; set; }

    public int Ordre { get; set; }
    /// <summary>Scène par laquelle la visite commence.</summary>
    public bool EstDepart { get; set; }

    // Orientation initiale de la vue (degrés)
    public double YawInitial { get; set; }
    public double PitchInitial { get; set; }
    public double HfovInitial { get; set; } = 100;

    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public List<Hotspot> Hotspots { get; set; } = [];
}

/// <summary>Point cliquable placé dans un panorama.</summary>
public class Hotspot
{
    public int Id { get; set; }
    public int PanoramaId { get; set; }
    public Panorama Panorama { get; set; } = null!;

    public TypeHotspot Type { get; set; } = TypeHotspot.Navigation;
    /// <summary>Panorama de destination pour un hotspot de navigation.</summary>
    public int? PanoramaCibleId { get; set; }
    public Panorama? PanoramaCible { get; set; }

    public double Pitch { get; set; }
    public double Yaw { get; set; }
    public string? Texte { get; set; }
}
