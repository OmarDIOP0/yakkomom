using Microsoft.AspNetCore.Mvc;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Accueil de l'espace admin : terrains par statut, clics WhatsApp, fiches à compléter.</summary>
[Route("admin")]
public class TableauDeBordController(ITableauDeBordService tableau) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int jours = 30, CancellationToken ct = default) =>
        View(await tableau.TableauAsync(jours, ct));
}
