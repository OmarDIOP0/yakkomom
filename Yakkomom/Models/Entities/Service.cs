namespace Yakkomom.Models.Entities;

/// <summary>Service proposé par Yakkomom (vente de terrains, construction…).</summary>
public class Service
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    /// <summary>Phrase courte affichée sur l'accueil.</summary>
    public string? Resume { get; set; }
    public string? Description { get; set; }
    /// <summary>Nom de l'icône SVG du sprite (ex. « terrain », « maison »).</summary>
    public string? Icone { get; set; }
    public string? CleImageCouverture { get; set; }
    /// <summary>Message pré-rempli du bouton « Demander ce service sur WhatsApp ».</summary>
    public string? MessageWhatsApp { get; set; }
    public int Ordre { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;

    public List<ServicePhoto> Photos { get; set; } = [];
}

/// <summary>Photo de réalisation associée à un service.</summary>
public class ServicePhoto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public string CleStockage { get; set; } = string.Empty;
    public string? CleVignette { get; set; }
    public string TypeMime { get; set; } = "image/webp";
    public long TailleOctets { get; set; }
    /// <summary>Identifiant d'envoi généré par le navigateur (anti-doublon en cas de reprise).</summary>
    public Guid? IdEnvoi { get; set; }
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    public string? CouleurDominante { get; set; }
    public string? Legende { get; set; }
    public int Ordre { get; set; }
}
