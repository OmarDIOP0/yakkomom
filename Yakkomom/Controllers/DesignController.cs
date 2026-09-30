using Microsoft.AspNetCore.Mvc;

namespace Yakkomom.Controllers;

/// <summary>Vitrine du design system. Uniquement en développement.</summary>
[Route("design")]
public class DesignController(IWebHostEnvironment environnement) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!environnement.IsDevelopment()) return NotFound();
        ViewData["Rubrique"] = "design";
        return View();
    }
}
