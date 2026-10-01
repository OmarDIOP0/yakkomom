using System.ComponentModel.DataAnnotations;

namespace Yakkomom.Models.ViewModels.Admin;

public class ParametresVm : IChampsContacts
{
    [Required(ErrorMessage = "Le nom du site est obligatoire.")]
    [StringLength(100)]
    [Display(Name = "Nom du site")]
    public string NomSite { get; set; } = "Yakkomom";

    [StringLength(200)]
    [Display(Name = "Slogan")]
    public string? Slogan { get; set; }

    // Contacts par défaut
    [Display(Name = "WhatsApp 1")] public string? WhatsApp1 { get; set; }
    [StringLength(60)][Display(Name = "Nom affiché")] public string? WhatsApp1Libelle { get; set; }
    [Display(Name = "WhatsApp 2")] public string? WhatsApp2 { get; set; }
    [StringLength(60)][Display(Name = "Nom affiché")] public string? WhatsApp2Libelle { get; set; }
    [Display(Name = "WhatsApp 3")] public string? WhatsApp3 { get; set; }
    [StringLength(60)][Display(Name = "Nom affiché")] public string? WhatsApp3Libelle { get; set; }

    // Validé (avec les numéros) par ContactsSaisie, pour afficher toutes les erreurs en une fois.
    [Display(Name = "E-mail de contact")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Le message WhatsApp est obligatoire.")]
    [StringLength(500)]
    [Display(Name = "Message WhatsApp pré-rempli (fiche terrain)")]
    public string MessageWhatsAppTerrain { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Adresse de l'agence")]
    public string? Adresse { get; set; }

    [StringLength(200)]
    [Display(Name = "Horaires d'ouverture")]
    public string? HorairesOuverture { get; set; }

    // Informations légales
    [StringLength(150)][Display(Name = "Raison sociale")]
    public string? RaisonSociale { get; set; }
    [StringLength(60)][Display(Name = "Forme juridique")]
    public string? FormeJuridique { get; set; }
    [StringLength(30)][Display(Name = "NINEA")]
    public string? Ninea { get; set; }
    [StringLength(60)][Display(Name = "RCCM")]
    public string? Rccm { get; set; }
    [StringLength(120)][Display(Name = "Responsable de la publication")]
    public string? ResponsablePublication { get; set; }

    [Url(ErrorMessage = "Lien invalide (commencez par https://).")][StringLength(300)][Display(Name = "Facebook")]
    public string? Facebook { get; set; }
    [Url(ErrorMessage = "Lien invalide (commencez par https://).")][StringLength(300)][Display(Name = "Instagram")]
    public string? Instagram { get; set; }
    [Url(ErrorMessage = "Lien invalide (commencez par https://).")][StringLength(300)][Display(Name = "TikTok")]
    public string? TikTok { get; set; }
    [Url(ErrorMessage = "Lien invalide (commencez par https://).")][StringLength(300)][Display(Name = "YouTube")]
    public string? YouTube { get; set; }
    [Url(ErrorMessage = "Lien invalide (commencez par https://).")][StringLength(300)][Display(Name = "LinkedIn")]
    public string? LinkedIn { get; set; }
}
