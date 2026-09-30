using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Securite;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

public class GestionComptesService(UserManager<ApplicationUser> utilisateurs, IJournalService journal) : IGestionComptesService
{
    public async Task<IReadOnlyList<UtilisateurLigneVm>> ListerAsync(string idUtilisateurCourant)
    {
        var superAdmins = (await utilisateurs.GetUsersInRoleAsync(Roles.SuperAdmin)).Select(u => u.Id).ToHashSet();
        var admins = (await utilisateurs.GetUsersInRoleAsync(Roles.Admin)).Select(u => u.Id).ToHashSet();
        var comptes = await utilisateurs.Users.AsNoTracking().OrderBy(u => u.NomComplet ?? u.Email).ToListAsync();

        return comptes
            .Where(u => superAdmins.Contains(u.Id) || admins.Contains(u.Id))
            .Select(u => new UtilisateurLigneVm
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? "",
                NomComplet = u.NomComplet,
                Role = superAdmins.Contains(u.Id) ? Roles.SuperAdmin : Roles.Admin,
                EstBloque = u.LockoutEnd is { } fin && fin > DateTimeOffset.UtcNow.AddYears(1),
                EstVerrouilleTemporairement = u.LockoutEnd is { } f && f > DateTimeOffset.UtcNow && f <= DateTimeOffset.UtcNow.AddYears(1),
                DoitChangerMotDePasse = u.DoitChangerMotDePasse,
                CreeLe = u.CreeLe,
                DerniereConnexion = u.DerniereConnexion,
                EstMoi = u.Id == idUtilisateurCourant
            })
            .ToList();
    }

    public async Task<ResultatCompte> CreerAsync(CreationUtilisateurVm vm)
    {
        if (!Roles.Tous.Contains(vm.Role)) return ResultatCompte.Echec("Rôle inconnu.");

        var email = vm.Email.Trim();
        if (await utilisateurs.FindByEmailAsync(email) is not null)
            return ResultatCompte.Echec("Un compte existe déjà avec cette adresse e-mail.");

        var motDePasse = GenererMotDePasse();
        var compte = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NomComplet = vm.NomComplet.Trim(),
            DoitChangerMotDePasse = true,
            LockoutEnabled = true
        };

        var creation = await utilisateurs.CreateAsync(compte, motDePasse);
        if (!creation.Succeeded) return ResultatCompte.Echec(string.Join(" ", creation.Errors.Select(e => e.Description)));
        await utilisateurs.AddToRoleAsync(compte, vm.Role);

        await journal.EnregistrerAsync(TypeAction.Creation, "Compte", compte.Id, $"Création du compte {email} ({vm.Role})");
        return ResultatCompte.Ok(new MotDePasseProvisoireVm
        {
            Email = email, NomComplet = compte.NomComplet, MotDePasse = motDePasse, EstNouveauCompte = true
        });
    }

    public async Task<ResultatCompte> ReinitialiserMotDePasseAsync(string id)
    {
        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return ResultatCompte.Echec("Compte introuvable.");

        var motDePasse = GenererMotDePasse();
        var jeton = await utilisateurs.GeneratePasswordResetTokenAsync(compte);
        var resultat = await utilisateurs.ResetPasswordAsync(compte, jeton, motDePasse);
        if (!resultat.Succeeded) return ResultatCompte.Echec(string.Join(" ", resultat.Errors.Select(e => e.Description)));

        compte.DoitChangerMotDePasse = true;
        await utilisateurs.UpdateAsync(compte);
        await utilisateurs.SetLockoutEndDateAsync(compte, null);
        await utilisateurs.ResetAccessFailedCountAsync(compte);

        await journal.EnregistrerAsync(TypeAction.Modification, "Compte", compte.Id, $"Réinitialisation du mot de passe de {compte.Email}");
        return ResultatCompte.Ok(new MotDePasseProvisoireVm { Email = compte.Email!, NomComplet = compte.NomComplet, MotDePasse = motDePasse });
    }

    public async Task<ResultatCompte> ChangerRoleAsync(string id, string role, string idUtilisateurCourant)
    {
        if (!Roles.Tous.Contains(role)) return ResultatCompte.Echec("Rôle inconnu.");
        if (id == idUtilisateurCourant) return ResultatCompte.Echec("Vous ne pouvez pas modifier votre propre rôle.");

        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return ResultatCompte.Echec("Compte introuvable.");

        var rolesActuels = await utilisateurs.GetRolesAsync(compte);
        if (rolesActuels.Contains(role)) return ResultatCompte.Ok();
        if (rolesActuels.Contains(Roles.SuperAdmin) && await EstDernierSuperAdminAsync())
            return ResultatCompte.Echec("Il doit toujours rester au moins un SuperAdmin.");

        await utilisateurs.RemoveFromRolesAsync(compte, rolesActuels.Intersect(Roles.Tous));
        await utilisateurs.AddToRoleAsync(compte, role);
        await utilisateurs.UpdateSecurityStampAsync(compte); // le nouveau rôle s'applique à sa prochaine requête

        await journal.EnregistrerAsync(TypeAction.Modification, "Compte", compte.Id, $"Rôle de {compte.Email} changé en {role}");
        return ResultatCompte.Ok();
    }

    public async Task<ResultatCompte> BloquerAsync(string id, bool bloquer, string idUtilisateurCourant)
    {
        if (id == idUtilisateurCourant) return ResultatCompte.Echec("Vous ne pouvez pas bloquer votre propre compte.");

        var compte = await utilisateurs.FindByIdAsync(id);
        if (compte is null) return ResultatCompte.Echec("Compte introuvable.");

        if (bloquer && await utilisateurs.IsInRoleAsync(compte, Roles.SuperAdmin) && await EstDernierSuperAdminAsync())
            return ResultatCompte.Echec("Il doit toujours rester au moins un SuperAdmin actif.");

        await utilisateurs.SetLockoutEnabledAsync(compte, true);
        await utilisateurs.SetLockoutEndDateAsync(compte, bloquer ? DateTimeOffset.MaxValue : null);
        if (bloquer) await utilisateurs.UpdateSecurityStampAsync(compte); // déconnexion sous 5 minutes
        else await utilisateurs.ResetAccessFailedCountAsync(compte);

        await journal.EnregistrerAsync(TypeAction.Modification, "Compte", compte.Id,
            bloquer ? $"Blocage du compte {compte.Email}" : $"Déblocage du compte {compte.Email}");
        return ResultatCompte.Ok();
    }

    private async Task<bool> EstDernierSuperAdminAsync()
    {
        var actifs = (await utilisateurs.GetUsersInRoleAsync(Roles.SuperAdmin))
            .Count(u => u.LockoutEnd is null || u.LockoutEnd < DateTimeOffset.UtcNow.AddYears(1));
        return actifs <= 1;
    }

    /// <summary>
    /// Mot de passe provisoire lisible et facile à dicter ou envoyer par WhatsApp :
    /// 3 groupes de 4 caractères sans caractères ambigus (0/O, 1/l/I), ex. « kx7m-p3wa-9rtd ».
    /// </summary>
    public static string GenererMotDePasse()
    {
        const string lettres = "abcdefghjkmnpqrstuvwxyz";
        const string chiffres = "23456789";
        const string alphabet = lettres + chiffres;

        char[] c;
        do
        {
            c = new char[12];
            for (var i = 0; i < c.Length; i++) c[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }
        // Règles Identity : au moins une minuscule, un chiffre et 4 caractères différents.
        while (!c.Any(char.IsLetter) || !c.Any(char.IsDigit) || c.Distinct().Count() < 4);

        var texte = new string(c);
        return $"{texte[..4]}-{texte[4..8]}-{texte[8..]}";
    }
}
