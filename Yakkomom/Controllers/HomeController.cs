using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Yakkomom.Models.ViewModels;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers
{
    public class HomeController(ITerrainPublicService terrains, IParametreSiteService parametres, Yakkomom.Services.Seo seo) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            ViewData["Rubrique"] = "accueil";
            var meta = new MetaPage();
            meta.DonneesStructurees.AddRange(seo.Organisation(await parametres.ObtenirAsync(ct)));
            ViewData[MetaPage.Cle] = meta;
            return View(await terrains.AccueilAsync(ct));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>Pages d'erreur HTTP (404…) servies via UseStatusCodePagesWithReExecute.</summary>
        [Route("erreur/{code:int}")]
        public IActionResult CodeStatut([FromRoute] int code)
        {
            // Page rejouée après une autre requête (parfois un envoi de fichier) : on ne se fie qu'à l'URL, bornée.
            if (code is < 400 or > 599) code = StatusCodes.Status404NotFound;
            Response.StatusCode = code;
            ViewData["Robots"] = "noindex";
            return View(code == 404 ? "Introuvable" : "Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
