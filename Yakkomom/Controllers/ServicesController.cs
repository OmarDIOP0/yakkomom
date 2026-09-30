using Microsoft.AspNetCore.Mvc;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>Pages publiques des services Yakkomom.</summary>
[Route("services")]
public class ServicesController(IServicesSiteService services, IParametreSiteService parametres, Yakkomom.Services.Seo seo) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Rubrique"] = "services";
        return View(await services.ListerPublicAsync(ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        var vm = await services.DetailPublicAsync(slug, ct);
        if (vm is null) return NotFound();
        ViewData["Rubrique"] = "services";
        var meta = new Models.ViewModels.MetaPage { Image = vm.ImagePartage };
        meta.DonneesStructurees.Add(seo.Service(vm, (await parametres.ObtenirAsync(ct)).NomSite));
        meta.DonneesStructurees.Add(seo.FilAriane(("Accueil", "/"), ("Services", "/services"), (vm.Titre, $"/services/{vm.Slug}")));
        ViewData[Models.ViewModels.MetaPage.Cle] = meta;
        return View(vm);
    }
}
