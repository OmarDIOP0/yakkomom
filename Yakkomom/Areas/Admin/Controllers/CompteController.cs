using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Securite;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

[Route("admin")]
[AutoriseAvecMotDePasseProvisoire]
public class CompteController(
    SignInManager<ApplicationUser> connexion,
    UserManager<ApplicationUser> utilisateurs,
    IJournalService journal,
    ILogger<CompteController> logger) : AdminControllerBase
{
    [HttpGet("connexion")]
    [AllowAnonymous]
    public IActionResult Connexion(string? retour = null)
    {
        if (User.Identity?.IsAuthenticated == true && (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin)))
            return RedirectLocal(retour);
        return View(new ConnexionVm { Retour = retour });
    }

    [HttpPost("connexion")]
    [AllowAnonymous]
    [EnableRateLimiting(Politiques.LimiteConnexion)]
    public async Task<IActionResult> Connexion(ConnexionVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        const string messageGenerique = "Adresse e-mail ou mot de passe incorrect.";
        var email = vm.Email.Trim();
        var resultat = await connexion.PasswordSignInAsync(email, vm.MotDePasse, vm.SeSouvenir, lockoutOnFailure: true);

        if (resultat.IsLockedOut)
        {
            logger.LogWarning("Connexion admin refusée (compte verrouillé) : {Email}", email);
            ModelState.AddModelError(string.Empty,
                "Compte temporairement verrouillé après plusieurs essais, ou bloqué par un administrateur. Réessayez dans 15 minutes.");
            return View(vm);
        }
        if (!resultat.Succeeded)
        {
            logger.LogWarning("Échec de connexion admin : {Email}", email);
            ModelState.AddModelError(string.Empty, messageGenerique);
            return View(vm);
        }

        var compte = await utilisateurs.FindByNameAsync(email);
        if (compte is null || !(await utilisateurs.GetRolesAsync(compte)).Any(Roles.Tous.Contains))
        {
            // Compte sans rôle admin : on ne lui ouvre pas la session.
            await connexion.SignOutAsync();
            ModelState.AddModelError(string.Empty, messageGenerique);
            return View(vm);
        }

        compte.DerniereConnexion = DateTime.UtcNow;
        await utilisateurs.UpdateAsync(compte);
        await journal.EnregistrerPourAsync(compte.Id, compte.Email, TypeAction.Connexion, "Compte", compte.Id, "Connexion à l'espace admin");

        return compte.DoitChangerMotDePasse
            ? RedirectToAction(nameof(MotDePasse))
            : RedirectLocal(vm.Retour);
    }

    [HttpPost("deconnexion")]
    public async Task<IActionResult> Deconnexion()
    {
        await connexion.SignOutAsync();
        return Redirect("/admin/connexion");
    }

    [HttpGet("acces-refuse")]
    [AllowAnonymous]
    public IActionResult AccesRefuse() => View();

    [HttpGet("mot-de-passe")]
    public IActionResult MotDePasse() =>
        View(new ChangementMotDePasseVm { Obligatoire = User.HasClaim(c => c.Type == ConfigurationSecurite.ClaimChangementMotDePasse) });

    [HttpPost("mot-de-passe")]
    public async Task<IActionResult> MotDePasse(ChangementMotDePasseVm vm)
    {
        vm.Obligatoire = User.HasClaim(c => c.Type == ConfigurationSecurite.ClaimChangementMotDePasse);
        if (!ModelState.IsValid) return View(vm);

        var compte = await utilisateurs.GetUserAsync(User);
        if (compte is null) return Redirect("/admin/connexion");

        if (vm.Nouveau == vm.Actuel)
        {
            ModelState.AddModelError(nameof(vm.Nouveau), "Le nouveau mot de passe doit être différent de l'actuel.");
            return View(vm);
        }

        var resultat = await utilisateurs.ChangePasswordAsync(compte, vm.Actuel, vm.Nouveau);
        if (!resultat.Succeeded)
        {
            foreach (var e in resultat.Errors)
                ModelState.AddModelError(e.Code == "PasswordMismatch" ? nameof(vm.Actuel) : nameof(vm.Nouveau), e.Description);
            return View(vm);
        }

        compte.DoitChangerMotDePasse = false;
        await utilisateurs.UpdateAsync(compte);
        await connexion.RefreshSignInAsync(compte); // régénère le cookie sans l'indicateur
        await journal.EnregistrerAsync(TypeAction.Modification, "Compte", compte.Id, "Changement de mot de passe");

        Succes("Mot de passe modifié.");
        return RedirectToAction("Index", "TableauDeBord");
    }

    private IActionResult RedirectLocal(string? retour) =>
        !string.IsNullOrEmpty(retour) && Url.IsLocalUrl(retour) && retour.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            ? LocalRedirect(retour)
            : RedirectToAction("Index", "TableauDeBord");
}
