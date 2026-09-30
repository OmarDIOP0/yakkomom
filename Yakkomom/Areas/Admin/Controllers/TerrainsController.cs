using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Securite;
using Yakkomom.Services;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Areas.Admin.Controllers;

[Route("admin/terrains")]
public partial class TerrainsController(
    ITerrainAdminService terrains,
    ILocaliteService localites,
    IParametreSiteService parametres,
    IMediaService medias,
    IVisite360Service visite360) : AdminControllerBase
{
    private const string Continuer = "continuer";

    // --- Liste --------------------------------------------------------------
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] FiltreTerrainsAdmin filtre, CancellationToken ct) =>
        View(await terrains.ListerAsync(filtre, ct));

    // --- Création : un titre suffit ----------------------------------------
    [HttpGet("nouveau")]
    public IActionResult Nouveau() => View(new NouveauTerrainVm());

    [HttpPost("nouveau")]
    public async Task<IActionResult> Nouveau(NouveauTerrainVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);
        var terrain = await terrains.CreerBrouillonAsync(vm.Titre, ct);
        Succes($"Brouillon {terrain.Reference} créé. Complétez la fiche à votre rythme : tout est facultatif.");
        return RedirectToAction(nameof(Infos), new { id = terrain.Id });
    }

    [HttpGet("{id:int}")]
    public IActionResult Modifier(int id) => RedirectToAction(nameof(Infos), new { id });

    // --- Onglet : informations générales ------------------------------------
    [HttpGet("{id:int}/infos")]
    public async Task<IActionResult> Infos(int id, CancellationToken ct)
    {
        var t = await terrains.ObtenirAsync(id, ct);
        if (t is null) return NotFound();
        return await Afficher(id, OngletTerrain.Infos, new InfosTerrainVm
        {
            Titre = t.Titre, Type = t.Type, Description = t.Description,
            Prix = Nombres.PourSaisie(t.Prix), PrixNegociable = t.PrixNegociable,
            SurfaceM2 = Nombres.PourSaisie(t.SurfaceM2), LongueurM = Nombres.PourSaisie(t.LongueurM), LargeurM = Nombres.PourSaisie(t.LargeurM),
            AccesEau = t.AccesEau, AccesElectricite = t.AccesElectricite, RouteAcces = t.RouteAcces,
            RouteAccesDetail = t.RouteAccesDetail, EstMisEnAvant = t.EstMisEnAvant,
            SurfaceCalculeeM2 = t.SurfaceCalculeeM2
        }, ct);
    }

    [HttpPost("{id:int}/infos")]
    public Task<IActionResult> Infos(int id, InfosTerrainVm vm, CancellationToken ct) =>
        Traiter(id, OngletTerrain.Infos, vm, () => terrains.EnregistrerInfosAsync(id, vm, ct), ct);

    // --- Onglet : localisation ------------------------------------------------
    [HttpGet("{id:int}/localisation")]
    public async Task<IActionResult> Localisation(int id, CancellationToken ct)
    {
        var t = await terrains.ObtenirAsync(id, ct);
        if (t is null) return NotFound();
        return await Afficher(id, OngletTerrain.Localisation, new LocalisationTerrainVm
        {
            RegionId = t.RegionId, DepartementId = t.DepartementId, CommuneId = t.CommuneId,
            QuartierVillage = t.QuartierVillage, Adresse = t.Adresse,
            Latitude = Nombres.PourSaisie(t.Latitude), Longitude = Nombres.PourSaisie(t.Longitude),
            ContourGeoJson = t.ContourGeoJson, SurfaceCalculeeM2 = t.SurfaceCalculeeM2,
            Commodites = t.Commodites.Select(c => new CommoditeSaisie { Libelle = c.Libelle, DistanceKm = Nombres.PourSaisie(c.DistanceKm) }).ToList()
        }, ct);
    }

    [HttpPost("{id:int}/localisation")]
    public Task<IActionResult> Localisation(int id, LocalisationTerrainVm vm, CancellationToken ct) =>
        Traiter(id, OngletTerrain.Localisation, vm, () => terrains.EnregistrerLocalisationAsync(id, vm, ct), ct);

    // --- Onglet : photos (envoi et gestion : TerrainsController.Medias.cs) ---
    [HttpGet("{id:int}/photos")]
    public Task<IActionResult> Photos(int id, CancellationToken ct) =>
        Afficher(id, OngletTerrain.Photos, new PhotosTerrainVm(), ct);

    // --- Onglet : visite 360° et vidéo --------------------------------------
    [HttpGet("{id:int}/visite-360")]
    public async Task<IActionResult> Visite360(int id, CancellationToken ct)
    {
        var t = await terrains.ObtenirAsync(id, ct);
        if (t is null) return NotFound();
        return await Afficher(id, OngletTerrain.Visite360, new MediasTerrainVm { VideoYoutubeUrl = t.VideoYoutubeUrl }, ct);
    }

    [HttpPost("{id:int}/visite-360")]
    public Task<IActionResult> Visite360(int id, MediasTerrainVm vm, CancellationToken ct) =>
        Traiter(id, OngletTerrain.Visite360, vm, () => terrains.EnregistrerMediasAsync(id, vm, ct), ct);

    // --- Onglet : documents fonciers ----------------------------------------
    [HttpGet("{id:int}/documents")]
    public async Task<IActionResult> Documents(int id, CancellationToken ct)
    {
        var t = await terrains.ObtenirAsync(id, ct);
        if (t is null) return NotFound();
        return await Afficher(id, OngletTerrain.Documents, new DocumentsTerrainVm { SituationFonciere = t.SituationFonciere }, ct);
    }

    [HttpPost("{id:int}/documents")]
    public Task<IActionResult> Documents(int id, DocumentsTerrainVm vm, CancellationToken ct) =>
        Traiter(id, OngletTerrain.Documents, vm, () => terrains.EnregistrerDocumentsAsync(id, vm, ct), ct);

    // --- Onglet : contacts ----------------------------------------------------
    [HttpGet("{id:int}/contacts")]
    public async Task<IActionResult> Contacts(int id, CancellationToken ct)
    {
        var t = await terrains.ObtenirAsync(id, ct);
        if (t is null) return NotFound();
        var c = t.Contacts;
        return await Afficher(id, OngletTerrain.Contacts, new ContactsTerrainVm
        {
            WhatsApp1 = TelephoneSenegal.Afficher(c.WhatsApp1), WhatsApp1Libelle = c.WhatsApp1Libelle,
            WhatsApp2 = TelephoneSenegal.Afficher(c.WhatsApp2), WhatsApp2Libelle = c.WhatsApp2Libelle,
            WhatsApp3 = TelephoneSenegal.Afficher(c.WhatsApp3), WhatsApp3Libelle = c.WhatsApp3Libelle,
            Email = c.Email
        }, ct);
    }

    [HttpPost("{id:int}/contacts")]
    public Task<IActionResult> Contacts(int id, ContactsTerrainVm vm, CancellationToken ct) =>
        Traiter(id, OngletTerrain.Contacts, vm, () => terrains.EnregistrerContactsAsync(id, vm, ct), ct);

    // --- Statut et suppression ----------------------------------------------
    [HttpPost("{id:int}/statut")]
    public async Task<IActionResult> Statut(int id, ChangementStatutVm vm, string? retour, CancellationToken ct)
    {
        var r = await terrains.ChangerStatutAsync(id, vm.Statut, ct);
        if (r.Reussi) Succes($"Statut changé : {Format.Statut(vm.Statut)}.");
        else Erreur(r.Erreur ?? "Changement de statut impossible.");
        return retour is not null && Url.IsLocalUrl(retour) ? LocalRedirect(retour) : RedirectToAction(nameof(Infos), new { id });
    }

    [HttpPost("{id:int}/supprimer")]
    [Authorize(Policy = Politiques.SuperAdmin)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
    {
        var r = await terrains.SupprimerAsync(id, ct);
        if (r.Reussi) Succes("Terrain supprimé définitivement.");
        else Erreur(r.Erreur ?? "Suppression impossible.");
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    private async Task<IActionResult> Afficher(int id, OngletTerrain onglet, OngletTerrainVm vm, CancellationToken ct)
    {
        vm.EnTete = await terrains.EnTeteAsync(id, onglet, ct);
        if (vm.EnTete is null) return NotFound();
        await Completer(vm, ct);
        return View(onglet.ToString(), vm);
    }

    private async Task<IActionResult> Traiter(int id, OngletTerrain onglet, OngletTerrainVm vm,
        Func<Task<ResultatOperation>> enregistrer, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var r = await enregistrer();
            if (r.Reussi)
            {
                Succes("Modifications enregistrées.");
                var suivant = vm.Suite == Continuer ? OngletSuivant(onglet) : onglet;
                return RedirectToAction(suivant.ToString(), new { id });
            }
            if (r.Erreur is not null) ModelState.AddModelError(string.Empty, r.Erreur);
            foreach (var (champ, message) in r.Erreurs) ModelState.AddModelError(champ, message);
        }
        return await Afficher(id, onglet, vm, ct);
    }

    private async Task Completer(OngletTerrainVm vm, CancellationToken ct)
    {
        switch (vm)
        {
            case LocalisationTerrainVm loc:
                loc.Localites = await localites.ListerAsync(ct);
                // Toujours quelques lignes vides pour ajouter des commodités sans JavaScript
                var cible = Math.Max(loc.Commodites.Count + 2, 5);
                while (loc.Commodites.Count < cible) loc.Commodites.Add(new CommoditeSaisie());
                break;
            case PhotosTerrainVm photos:
                photos.Photos = await medias.ListerPhotosAsync(photos.EnTete!.Id, ct);
                break;
            case MediasTerrainVm medias360 when medias360.EnTete!.OngletActif == OngletTerrain.Visite360:
                medias360.Panoramas = await visite360.ListerAsync(medias360.EnTete.Id, ct);
                medias360.ConfigEdition = await visite360.ConfigurationAsync(medias360.EnTete.Id, hd: false, edition: true, ct);
                break;
            case DocumentsTerrainVm documents:
                documents.Documents = await medias.ListerDocumentsAsync(documents.EnTete!.Id, ct);
                break;
            case ContactsTerrainVm contacts:
                contacts.ContactsParDefaut = (await parametres.ObtenirAsync(ct)).ContactsParDefaut;
                break;
        }
    }

    private static OngletTerrain OngletSuivant(OngletTerrain o) =>
        o == OngletTerrain.Contacts ? OngletTerrain.Contacts : (OngletTerrain)((int)o + 1);
}
