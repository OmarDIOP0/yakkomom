using System.ComponentModel.DataAnnotations;
using Yakkomom.Securite;

namespace Yakkomom.Models.ViewModels.Admin;

public class ConnexionVm
{
    [Required(ErrorMessage = "Saisissez votre adresse e-mail.")]
    [EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
    [Display(Name = "Adresse e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Saisissez votre mot de passe.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string MotDePasse { get; set; } = string.Empty;

    [Display(Name = "Rester connecté sur cet appareil")]
    public bool SeSouvenir { get; set; }

    public string? Retour { get; set; }
}

public class ChangementMotDePasseVm
{
    [Required(ErrorMessage = "Saisissez votre mot de passe actuel.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe actuel")]
    public string Actuel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choisissez un nouveau mot de passe.")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Au moins 10 caractères.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nouveau mot de passe")]
    public string Nouveau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmez le nouveau mot de passe.")]
    [Compare(nameof(Nouveau), ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmer le nouveau mot de passe")]
    public string Confirmation { get; set; } = string.Empty;

    public bool Obligatoire { get; set; }
}

public class UtilisateurLigneVm
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? NomComplet { get; init; }
    public required string Role { get; init; }
    public bool EstBloque { get; init; }
    /// <summary>Verrouillage temporaire après trop d'échecs de connexion.</summary>
    public bool EstVerrouilleTemporairement { get; init; }
    public bool DoitChangerMotDePasse { get; init; }
    public DateTime CreeLe { get; init; }
    public DateTime? DerniereConnexion { get; init; }
    public bool EstMoi { get; init; }
}

public class CreationUtilisateurVm
{
    [Required(ErrorMessage = "Saisissez le nom.")]
    [StringLength(100)]
    [Display(Name = "Nom complet")]
    public string NomComplet { get; set; } = string.Empty;

    [Required(ErrorMessage = "Saisissez l'adresse e-mail.")]
    [EmailAddress(ErrorMessage = "Adresse e-mail invalide.")]
    [Display(Name = "Adresse e-mail (identifiant de connexion)")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rôle")]
    public string Role { get; set; } = Roles.Admin;
}

/// <summary>Affiché une seule fois après création ou réinitialisation.</summary>
public class MotDePasseProvisoireVm
{
    public required string Email { get; init; }
    public string? NomComplet { get; init; }
    public required string MotDePasse { get; init; }
    public bool EstNouveauCompte { get; init; }
}
