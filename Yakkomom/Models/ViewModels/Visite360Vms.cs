using Yakkomom.Models.Enums;

namespace Yakkomom.Models.ViewModels;

public class PanoramaAdminVm
{
    public int Id { get; init; }
    public required string Titre { get; init; }
    public required string UrlVignette { get; init; }
    public long TailleHd { get; init; }
    public long TailleBd { get; init; }
    public int Largeur { get; init; }
    public int Hauteur { get; init; }
    public double Vaov { get; init; }
    public bool EstDepart { get; init; }
    public int Ordre { get; init; }
    public int NombreHotspots { get; init; }
}

public class EnvoiPanorama
{
    public Guid? IdEnvoi { get; set; }
    public string? Titre { get; set; }
}

public record VueInitiale(double Yaw, double Pitch, double Hfov);

public record NouveauHotspot(double Pitch, double Yaw, TypeHotspot Type, int? CibleId, string? Texte);

/// <summary>Visite prête pour la fiche publique (rien n'est chargé avant le clic).</summary>
public class VisitePubliqueVm
{
    public required string UrlVignette { get; init; }
    public required string TitreDepart { get; init; }
    public int NombreScenes { get; init; }
    /// <summary>Poids de la première scène en version légère (ce que coûte le lancement).</summary>
    public long TailleLancement { get; init; }
    /// <summary>Configuration Pannellum (JSON) en version légère.</summary>
    public required string ConfigJson { get; init; }
    /// <summary>{ idScène: urlHD } pour le bouton « Haute définition ».</summary>
    public required string UrlsHdJson { get; init; }
    public long TailleHdDepart { get; init; }
}
