using Yakkomom.Models.ViewModels.Admin;

namespace Yakkomom.Services.Interfaces;

public record ResultatCompte(bool Reussi, string? Erreur = null, MotDePasseProvisoireVm? MotDePasse = null)
{
    public static ResultatCompte Ok(MotDePasseProvisoireVm? mdp = null) => new(true, null, mdp);
    public static ResultatCompte Echec(string erreur) => new(false, erreur);
}

/// <summary>Gestion des comptes admin par le SuperAdmin (pas d'inscription publique).</summary>
public interface IGestionComptesService
{
    Task<IReadOnlyList<UtilisateurLigneVm>> ListerAsync(string idUtilisateurCourant);
    Task<ResultatCompte> CreerAsync(CreationUtilisateurVm vm);
    Task<ResultatCompte> ReinitialiserMotDePasseAsync(string id);
    Task<ResultatCompte> ChangerRoleAsync(string id, string role, string idUtilisateurCourant);
    Task<ResultatCompte> BloquerAsync(string id, bool bloquer, string idUtilisateurCourant);
}
