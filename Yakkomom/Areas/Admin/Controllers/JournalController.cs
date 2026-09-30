using Microsoft.AspNetCore.Mvc;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Securite;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Journal des actions : qui a modifié quoi et quand (lecture seule, aucune suppression possible).</summary>
[Route("admin/journal")]
public class JournalController(ITableauDeBordService tableau) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] FiltreJournal filtre, CancellationToken ct) =>
        View(await tableau.JournalAsync(filtre, User.IsInRole(Roles.SuperAdmin), ct));
}
