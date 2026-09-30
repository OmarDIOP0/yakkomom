using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Securite;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Comptes administrateurs : réservé au SuperAdmin.</summary>
[Route("admin/utilisateurs")]
[Authorize(Policy = Politiques.SuperAdmin)]
public class UtilisateursController(IGestionComptesService comptes) : AdminControllerBase
{
    private string MonId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await comptes.ListerAsync(MonId));

    [HttpGet("nouveau")]
    public IActionResult Creer() => View(new CreationUtilisateurVm());

    [HttpPost("nouveau")]
    public async Task<IActionResult> Creer(CreationUtilisateurVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var resultat = await comptes.CreerAsync(vm);
        if (!resultat.Reussi)
        {
            ModelState.AddModelError(string.Empty, resultat.Erreur!);
            return View(vm);
        }
        // Affiché directement (jamais stocké) : une seule fois.
        return View("MotDePasseProvisoire", resultat.MotDePasse);
    }

    [HttpPost("{id}/mot-de-passe")]
    public async Task<IActionResult> ReinitialiserMotDePasse(string id)
    {
        var resultat = await comptes.ReinitialiserMotDePasseAsync(id);
        if (resultat.Reussi) return View("MotDePasseProvisoire", resultat.MotDePasse);
        Erreur(resultat.Erreur!);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/role")]
    public async Task<IActionResult> ChangerRole(string id, string role)
    {
        var resultat = await comptes.ChangerRoleAsync(id, role, MonId);
        if (resultat.Reussi) Succes("Rôle mis à jour."); else Erreur(resultat.Erreur!);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/blocage")]
    public async Task<IActionResult> Bloquer(string id, bool bloquer)
    {
        var resultat = await comptes.BloquerAsync(id, bloquer, MonId);
        if (resultat.Reussi) Succes(bloquer ? "Compte bloqué : il sera déconnecté sous 5 minutes." : "Compte débloqué.");
        else Erreur(resultat.Erreur!);
        return RedirectToAction(nameof(Index));
    }
}
