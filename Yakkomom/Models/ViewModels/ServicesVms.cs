using System.ComponentModel.DataAnnotations;
using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Models.ViewModels;

// --- Public -------------------------------------------------------------------
public record ServiceCarteVm(string Titre, string Slug, string? Resume, string? Icone, PhotoPubliqueVm? Couverture, int NombrePhotos);

public class ServicePublicVm
{
    public required string Titre { get; init; }
    public required string Slug { get; init; }
    public string? Resume { get; init; }
    public string? Description { get; init; }
    public string? Icone { get; init; }
    public IReadOnlyList<PhotoPubliqueVm> Photos { get; init; } = [];
    public IReadOnlyList<ContactWhatsAppVm> WhatsApp { get; init; } = [];
    public string? Email { get; init; }
    public IReadOnlyList<ServiceCarteVm> Autres { get; init; } = [];
    public ImagePartage? ImagePartage { get; init; }
}

// --- Admin --------------------------------------------------------------------
public class ServiceLigneVm
{
    public int Id { get; init; }
    public required string Titre { get; init; }
    public required string Slug { get; init; }
    public string? Resume { get; init; }
    public string? Icone { get; init; }
    public bool EstActif { get; init; }
    public int NombrePhotos { get; init; }
    public string? UrlCouverture { get; init; }
}

public class ServiceEditionVm
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(120)]
    [Display(Name = "Titre")]
    public string Titre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Résumé (une phrase, affichée sur l'accueil)")]
    public string? Resume { get; set; }

    [StringLength(8000)]
    [Display(Name = "Description détaillée")]
    public string? Description { get; set; }

    [StringLength(40)]
    [Display(Name = "Icône")]
    public string? Icone { get; set; }

    [StringLength(500)]
    [Display(Name = "Message WhatsApp pré-rempli")]
    public string? MessageWhatsApp { get; set; }

    [Display(Name = "Visible sur le site")]
    public bool EstActif { get; set; } = true;

    public IReadOnlyList<PhotoAdminVm> Photos { get; set; } = [];

    /// <summary>Icônes disponibles dans le sprite (nom technique, libellé).</summary>
    public static readonly (string Nom, string Libelle)[] Icones =
    [
        ("terrain", "Terrain"), ("maison", "Maison"), ("batiment", "Bâtiment"), ("accompagnement", "Personnes"),
        ("bouclier", "Bouclier (sécurité)"), ("croissance", "Croissance"), ("champ", "Champ / plante"), ("document", "Document"),
        ("position", "Repère"), ("carte", "Carte"), ("route", "Route"), ("eau", "Eau"), ("electricite", "Électricité"),
        ("surface", "Surface"), ("vue360", "360°"), ("camera", "Appareil photo")
    ];
}

public class NouveauServiceVm
{
    [Required(ErrorMessage = "Donnez un titre au service.")]
    [StringLength(120)]
    [Display(Name = "Titre du service")]
    public string Titre { get; set; } = string.Empty;
}

/// <summary>Formulaire de la page Contact : compose un message WhatsApp (rien n'est enregistré).</summary>
public class DemandeContactVm
{
    public string? Nom { get; set; }
    public string? Besoin { get; set; }
    public string? Zone { get; set; }
    public string? Budget { get; set; }
    public string? Message { get; set; }
}
