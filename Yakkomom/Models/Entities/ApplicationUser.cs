using Microsoft.AspNetCore.Identity;

namespace Yakkomom.Models.Entities;

/// <summary>Compte administrateur (pas d'inscription publique).</summary>
public class ApplicationUser : IdentityUser
{
    public string? NomComplet { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public DateTime? DerniereConnexion { get; set; }
    /// <summary>Compte créé avec un mot de passe provisoire : changement obligatoire à la connexion.</summary>
    public bool DoitChangerMotDePasse { get; set; }
}
