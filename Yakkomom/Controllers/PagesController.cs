using Microsoft.AspNetCore.Mvc;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>Pages de contenu : À propos, Acheter en toute sécurité, Contact, Mentions légales, CGU.</summary>
public class PagesController(IParametreSiteService parametres) : Controller
{
    [HttpGet("a-propos")]
    public async Task<IActionResult> APropos(CancellationToken ct)
    {
        ViewData["Rubrique"] = "apropos";
        return View(await parametres.ObtenirAsync(ct));
    }

    [HttpGet("acheter-en-securite")]
    public IActionResult AcheterEnSecurite()
    {
        ViewData["Rubrique"] = "securite";
        return View();
    }

    [HttpGet("contact")]
    public async Task<IActionResult> Contact(CancellationToken ct)
    {
        ViewData["Rubrique"] = "contact";
        return View(await parametres.ObtenirAsync(ct));
    }

    [HttpGet("mentions-legales")]
    public async Task<IActionResult> MentionsLegales(CancellationToken ct) =>
        View(await parametres.ObtenirAsync(ct));

    [HttpGet("conditions-utilisation")]
    public async Task<IActionResult> ConditionsUtilisation(CancellationToken ct) =>
        View(await parametres.ObtenirAsync(ct));
}
