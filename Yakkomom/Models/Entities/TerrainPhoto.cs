namespace Yakkomom.Models.Entities;

public class TerrainPhoto
{
    public int Id { get; set; }
    public int TerrainId { get; set; }
    public Terrain Terrain { get; set; } = null!;

    /// <summary>Clé dans le stockage (IStorageService) — jamais une URL absolue.</summary>
    public string CleStockage { get; set; } = string.Empty;
    /// <summary>Vignette (~480 px) produite par le navigateur ; null si non fournie.</summary>
    public string? CleVignette { get; set; }
    public string TypeMime { get; set; } = "image/webp";
    /// <summary>Identifiant d'envoi généré par le navigateur : un renvoi après coupure ne crée pas de doublon.</summary>
    public Guid? IdEnvoi { get; set; }
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    public long TailleOctets { get; set; }
    /// <summary>Couleur dominante (#RRGGBB) affichée pendant le chargement.</summary>
    public string? CouleurDominante { get; set; }
    public string? Legende { get; set; }
    public int Ordre { get; set; }
    public bool EstCouverture { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
}
