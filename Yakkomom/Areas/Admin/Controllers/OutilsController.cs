using Microsoft.AspNetCore.Mvc;
using Yakkomom.Helpers;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>Petits services pour les scripts de l'admin.</summary>
[Route("admin/outils")]
public class OutilsController(IHttpClientFactory clients, ILogger<OutilsController> logger) : AdminControllerBase
{
    /// <summary>
    /// Lien court Google Maps (« maps.app.goo.gl/… », celui du bouton Partager du téléphone) → position GPS.
    /// Seules les redirections vers les domaines de Google sont suivies, 5 au plus.
    /// </summary>
    [HttpGet("lien-carte")]
    public async Task<IActionResult> LienCarte(string? url, CancellationToken ct)
    {
        if (Helpers.LienCarte.Extraire(url) is { } direct) return Ok(new { lat = direct.Lat, lng = direct.Lng });
        if (!Helpers.LienCarte.EstLienCourt(url)) return BadRequest(new { erreur = "Lien non reconnu : collez un lien Google Maps ou des coordonnées." });

        var client = clients.CreateClient(nameof(LienCarte));
        var adresse = new Uri(url!.Trim());
        try
        {
            for (var i = 0; i < 5; i++)
            {
                using var reponse = await client.GetAsync(adresse, HttpCompletionOption.ResponseHeadersRead, ct);
                var suivante = reponse.Headers.Location is { } l ? new Uri(adresse, l) : null;
                if (Helpers.LienCarte.Extraire(suivante?.ToString() ?? adresse.ToString()) is { } c)
                    return Ok(new { lat = c.Lat, lng = c.Lng });
                if (suivante is null || !Helpers.LienCarte.HoteAutorise(suivante)) break;
                adresse = suivante;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation(ex, "Lien Google Maps non résolu.");
        }
        return UnprocessableEntity(new { erreur = "Position introuvable dans ce lien : ouvrez-le dans Google Maps, puis copiez les chiffres de la position (ex. 14.5198, -17.0021)." });
    }
}
