using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Services;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Controllers;

/// <summary>
/// Redirections WhatsApp avec suivi : /wa/YK-0042/1?source=fiche → enregistre le clic
/// (terrain, numéro, source, date — aucune donnée personnelle) puis redirige vers wa.me
/// avec le message pré-rempli. Fonctionne sans JavaScript.
/// </summary>
[Route("wa")]
public class WhatsAppController(
    YakkomomDbContext db,
    IParametreSiteService parametres,
    UrlSite urls,
    IMemoryCache cache,
    ILogger<WhatsAppController> logger) : Controller
{
    /// <summary>Un même visiteur sur le même bouton n'est compté qu'une fois pendant cette durée.</summary>
    internal static readonly TimeSpan FenetreDoublons = TimeSpan.FromMinutes(10);

    [HttpGet("{reference:regex(^YK-\\d{{4,}}$)}/{index:int:range(1,3)}")]
    public async Task<IActionResult> Terrain(string reference, int index, string? source, string? motif, string? lot, CancellationToken ct)
    {
        var t = await db.Terrains.AsNoTracking()
            .Where(x => x.Reference == reference && (x.Statut == StatutTerrain.Disponible || x.Statut == StatutTerrain.Reserve || x.Statut == StatutTerrain.Vendu))
            .Select(x => new { x.Id, x.Reference, x.SurfaceM2, x.QuartierVillage, Commune = x.Commune != null ? x.Commune.Nom : null, x.Contacts })
            .FirstOrDefaultAsync(ct);
        if (t is null) return NotFound();

        var p = await parametres.ObtenirAsync(ct);
        var numero = WhatsAppLiens.NumerosEffectifs(t.Contacts, p.ContactsParDefaut).FirstOrDefault(n => n.Index == index);
        if (numero is null) return NotFound();

        var lien = urls.Absolue(UrlTerrain.Chemin(t.Reference, t.SurfaceM2, t.Commune));
        var resume = WhatsAppLiens.Resume(t.SurfaceM2, t.Commune ?? t.QuartierVillage);

        // Lot d'un lotissement : le message précise le lot (« lot 12, 300 m² à Diass »). Numéro inconnu : message du terrain.
        if (!string.IsNullOrWhiteSpace(lot) && lot.Length <= 20 &&
            await db.Lots.AsNoTracking().Where(l => l.TerrainId == t.Id && l.Numero == lot)
                .Select(l => new { l.Numero, l.SurfaceM2 }).FirstOrDefaultAsync(ct) is { } choisi)
            resume = $"lot {choisi.Numero}, " + WhatsAppLiens.Resume(choisi.SurfaceM2, t.Commune ?? t.QuartierVillage);

        var message = WhatsAppLiens.Message(motif == "document" ? MotifWhatsApp.Document : MotifWhatsApp.Terrain, p.NomSite,
            p.MessageWhatsAppTerrain, t.Reference, resume, lien);

        await EnregistrerClicAsync(t.Id, null, index, LireSource(source), ct);
        return Rediriger(WhatsAppLiens.UrlWhatsApp(numero.Numero, message));
    }

    [HttpGet("general/{index:int:range(1,3)}")]
    public async Task<IActionResult> General(int index, string? source, CancellationToken ct)
    {
        var p = await parametres.ObtenirAsync(ct);
        var numero = WhatsAppLiens.NumerosEffectifs(null, p.ContactsParDefaut).FirstOrDefault(n => n.Index == index);
        if (numero is null) return Redirect("/");

        var origine = LireSource(source);
        await EnregistrerClicAsync(null, null, index, origine == SourceClicWhatsApp.FicheTerrain ? SourceClicWhatsApp.General : origine, ct);
        return Rediriger(WhatsAppLiens.UrlWhatsApp(numero.Numero, WhatsAppLiens.Message(MotifWhatsApp.General, p.NomSite, "", "", "", "")));
    }

    /// <summary>Bouton « Demander ce service sur WhatsApp ».</summary>
    [HttpGet("service/{slug}/{index:int:range(1,3)}")]
    public async Task<IActionResult> Service(string slug, int index, CancellationToken ct)
    {
        var service = await db.Services.AsNoTracking().Where(s => s.Slug == slug && s.EstActif)
            .Select(s => new { s.Id, s.Titre, s.MessageWhatsApp }).FirstOrDefaultAsync(ct);
        if (service is null) return NotFound();

        var p = await parametres.ObtenirAsync(ct);
        var numero = WhatsAppLiens.NumerosEffectifs(null, p.ContactsParDefaut).FirstOrDefault(n => n.Index == index);
        if (numero is null) return Redirect("/contact");

        var message = (service.MessageWhatsApp ?? "Bonjour {site}, je souhaite en savoir plus sur votre service « " + service.Titre + " ».")
            .Replace("{site}", p.NomSite);
        await EnregistrerClicAsync(null, service.Id, index, SourceClicWhatsApp.Service, ct);
        return Rediriger(WhatsAppLiens.UrlWhatsApp(numero.Numero, message));
    }

    /// <summary>
    /// Formulaire de la page Contact : le message est composé ici à partir de champs courts
    /// (jamais un texte arbitraire fourni tel quel), puis ouvert dans WhatsApp. Rien n'est enregistré.
    /// </summary>
    [HttpGet("demande/{index:int:range(1,3)}")]
    public async Task<IActionResult> Demande(int index, Models.ViewModels.DemandeContactVm d, CancellationToken ct)
    {
        var p = await parametres.ObtenirAsync(ct);
        var numero = WhatsAppLiens.NumerosEffectifs(null, p.ContactsParDefaut).FirstOrDefault(n => n.Index == index);
        if (numero is null) return Redirect("/contact");

        static string? Court(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > max ? s.Trim()[..max] : s.Trim());
        var lignes = new List<string> { $"Bonjour {p.NomSite}," };
        if (Court(d.Nom, 80) is { } nom) lignes.Add($"je m'appelle {nom}.");
        lignes.Add(Court(d.Besoin, 80) is { } besoin ? $"Je suis intéressé(e) par : {besoin}." : "Je souhaite des informations.");
        if (Court(d.Zone, 80) is { } zone) lignes.Add($"Zone souhaitée : {zone}.");
        if (Court(d.Budget, 60) is { } budget) lignes.Add($"Budget : {budget}.");
        if (Court(d.Message, 600) is { } message) lignes.Add(message);

        await EnregistrerClicAsync(null, null, index, SourceClicWhatsApp.General, ct);
        return Rediriger(WhatsAppLiens.UrlWhatsApp(numero.Numero, string.Join("\n", lignes)));
    }

    private IActionResult Rediriger(string url)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        return Redirect(url);
    }

    private async Task EnregistrerClicAsync(int? terrainId, int? serviceId, int index, SourceClicWhatsApp source, CancellationToken ct)
    {
        if (EstPrechargementOuRobot()) return;

        // Anti-gonflement : empreinte irréversible (IP + bouton) gardée 10 min en mémoire, jamais en base.
        var empreinte = EmpreinteClic(HttpContext.Connection.RemoteIpAddress?.ToString(), terrainId, serviceId, index);
        if (cache.TryGetValue(empreinte, out _)) return;
        cache.Set(empreinte, true, FenetreDoublons);

        try
        {
            db.ClicsWhatsApp.Add(new ClicWhatsApp { TerrainId = terrainId, ServiceId = serviceId, NumeroIndex = (short)index, Source = source });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Une statistique perdue ne doit jamais empêcher le client de nous écrire.
            logger.LogWarning(ex, "Clic WhatsApp non enregistré.");
        }
    }

    internal static string EmpreinteClic(string? ip, int? terrainId, int? serviceId, int index) =>
        "clic:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ip}|{terrainId}|{serviceId}|{index}")))[..24];

    private static SourceClicWhatsApp LireSource(string? source) => source switch
    {
        "flottant" => SourceClicWhatsApp.BoutonFlottant,
        "liste" => SourceClicWhatsApp.Liste,
        "service" => SourceClicWhatsApp.Service,
        "general" => SourceClicWhatsApp.General,
        _ => SourceClicWhatsApp.FicheTerrain
    };

    /// <summary>Préchargements du navigateur et robots : pas de clic comptabilisé.</summary>
    private bool EstPrechargementOuRobot()
    {
        var h = Request.Headers;
        if (h["Sec-Purpose"].ToString().Contains("prefetch") || h["Purpose"].ToString().Contains("prefetch")) return true;
        var ua = h.UserAgent.ToString();
        return ua.Length == 0 || ua.Contains("bot", StringComparison.OrdinalIgnoreCase) || ua.Contains("spider", StringComparison.OrdinalIgnoreCase)
               || ua.Contains("crawl", StringComparison.OrdinalIgnoreCase) || ua.Contains("facebookexternalhit", StringComparison.OrdinalIgnoreCase)
               || ua.Contains("preview", StringComparison.OrdinalIgnoreCase);
    }
}
