using Microsoft.AspNetCore.Mvc;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Accueil de l'espace admin (statistiques complètes à l'étape 12).</summary>
[Route("admin")]
public class TableauDeBordController : AdminControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
