using Microsoft.AspNetCore.Mvc;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Services;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Areas.Admin.Controllers;

[Route("admin/parametres")]
public class ParametresController(IParametreSiteService parametres, IMediaService medias) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var p = await parametres.ObtenirAsync(ct);
        var c = p.ContactsParDefaut;
        return View(new ParametresVm
        {
            NomSite = p.NomSite, Slogan = p.Slogan,
            WhatsApp1 = TelephoneSenegal.Afficher(c.WhatsApp1), WhatsApp1Libelle = c.WhatsApp1Libelle,
            WhatsApp2 = TelephoneSenegal.Afficher(c.WhatsApp2), WhatsApp2Libelle = c.WhatsApp2Libelle,
            WhatsApp3 = TelephoneSenegal.Afficher(c.WhatsApp3), WhatsApp3Libelle = c.WhatsApp3Libelle,
            Email = c.Email, MessageWhatsAppTerrain = p.MessageWhatsAppTerrain,
            Adresse = p.Adresse, HorairesOuverture = p.HorairesOuverture,
            Facebook = p.Facebook, Instagram = p.Instagram, TikTok = p.TikTok, YouTube = p.YouTube, LinkedIn = p.LinkedIn
        });
    }

    [HttpPost("")]
    public async Task<IActionResult> Index(ParametresVm vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var r = await parametres.EnregistrerAsync(vm, ct);
            if (r.Reussi)
            {
                Succes("Paramètres enregistrés.");
                return RedirectToAction(nameof(Index));
            }
            foreach (var (champ, message) in r.Erreurs) ModelState.AddModelError(champ, message);
        }
        else
        {
            // Afficher aussi les erreurs de numéros / e-mail dès le premier envoi
            var controle = ContactsSaisie.Appliquer(new Contact(),
                (vm.WhatsApp1, vm.WhatsApp1Libelle), (vm.WhatsApp2, vm.WhatsApp2Libelle), (vm.WhatsApp3, vm.WhatsApp3Libelle), vm.Email);
            foreach (var (champ, message) in controle.Erreurs) ModelState.AddModelError(champ, message);
        }
        return View(vm);
    }

    [HttpPost("logo")]
    [RequestSizeLimit(ReglesEnvoi.LogoTailleMax + 64 * 1024)]
    public async Task<IActionResult> Logo(IFormFile? logo, CancellationToken ct)
    {
        var r = await medias.DefinirLogoAsync(logo, ct);
        if (r.Reussi) Succes("Logo mis à jour."); else Erreur(r.Erreur!);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("logo/supprimer")]
    public async Task<IActionResult> SupprimerLogo(CancellationToken ct)
    {
        await medias.SupprimerLogoAsync(ct);
        Succes("Logo retiré : le logo par défaut est de nouveau utilisé.");
        return RedirectToAction(nameof(Index));
    }
}
