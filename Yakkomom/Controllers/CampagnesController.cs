using Microsoft.AspNetCore.Mvc;

namespace Yakkomom.Controllers;

/// <summary>
/// Adresses courtes imprimées dans les QR codes (affiche, panneau, web, réseaux sociaux).
/// Elles redirigent vers les terrains avec les paramètres UTM du support : l'adresse imprimée ne change jamais,
/// la destination peut évoluer ici sans réimprimer. Redirection temporaire (302) pour garder cette liberté.
/// </summary>
public class CampagnesController : Controller
{
    private static readonly Dictionary<string, (string Source, string Support)> Supports = new(StringComparer.OrdinalIgnoreCase)
    {
        ["affiche"] = ("affiche", "print"),
        ["panneau"] = ("panneau", "outdoor"),
        ["web"] = ("site", "web"),
        ["reseaux"] = ("reseaux", "social"),
    };

    [HttpGet("{support:regex(^(affiche|panneau|web|reseaux)$)}")]
    public IActionResult Rediriger(string support)
    {
        var (source, medium) = Supports[support];
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex";
        return Redirect($"/terrains?utm_source={source}&utm_medium={medium}&utm_campaign=lancement");
    }
}
