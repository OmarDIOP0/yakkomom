using Microsoft.AspNetCore.Mvc;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>Pages de contenu : À propos, Acheter en toute sécurité, Contact.</summary>
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
}
