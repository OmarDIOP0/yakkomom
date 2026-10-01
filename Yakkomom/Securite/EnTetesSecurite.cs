using System.Security.Cryptography;

namespace Yakkomom.Securite;

/// <summary>
/// En-têtes de sécurité HTTP sur toutes les réponses, dont la politique de sécurité du contenu (CSP).
/// Seules les sources réellement utilisées sont autorisées :
/// <list type="bullet">
///   <item>scripts : le site lui-même (Leaflet et Pannellum sont auto-hébergés) + l'import map, autorisée par un nonce ;</item>
///   <item>images : Cloudinary, tuiles de carte Esri et OpenStreetMap, miniatures YouTube ;</item>
///   <item>cadres : lecteur YouTube sans cookie uniquement ; le site ne peut lui-même être affiché dans un cadre ;</item>
///   <item>formulaires : le site, et WhatsApp (le formulaire de contact redirige vers wa.me).</item>
/// </list>
/// Les blocs JSON (données structurées, configuration de la visite 360°) ne sont pas exécutés et ne sont pas
/// concernés par la CSP ; ils sont encodés par System.Text.Json (« &lt; » → « < »).
/// </summary>
public static class EnTetesSecurite
{
    private const string CleNonce = "yk.csp-nonce";

    /// <summary>Nonce de la requête, à poser sur les rares &lt;script&gt; en ligne (import map).</summary>
    public static string NonceCsp(this HttpContext contexte) =>
        contexte.Items.TryGetValue(CleNonce, out var n) && n is string s ? s : "";

    public static string Politique(string nonce, bool developpement) => string.Join("; ",
    [
        "default-src 'self'",
        $"script-src 'self' 'nonce-{nonce}'",
        // Attributs style="" (jauges, largeurs calculées) et styles posés par Leaflet/Pannellum : risque faible, pas de script.
        "style-src 'self' 'unsafe-inline'",
        "img-src 'self' data: blob: https://res.cloudinary.com https://server.arcgisonline.com https://tile.openstreetmap.org https://i.ytimg.com",
        // Le service worker met en cache les photos Cloudinary
        "connect-src 'self' https://res.cloudinary.com",
        "media-src 'self' https://res.cloudinary.com",
        "font-src 'self'",
        "frame-src https://www.youtube-nocookie.com",
        "worker-src 'self'",
        "manifest-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self' https://wa.me https://api.whatsapp.com",
        "frame-ancestors 'none'",
        .. developpement ? Array.Empty<string>() : ["upgrade-insecure-requests"]
    ]);

    public static IApplicationBuilder UseEnTetesSecurite(this IApplicationBuilder app, bool developpement) =>
        app.Use((contexte, suivant) =>
        {
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); // hexadécimal : aucun caractère à échapper en HTML
            contexte.Items[CleNonce] = nonce;

            contexte.Response.OnStarting(() =>
            {
                var h = contexte.Response.Headers;
                // La page d'erreur détaillée du développement utilise des scripts en ligne : on la laisse fonctionner.
                if (!(developpement && contexte.Response.StatusCode >= 500))
                    h.ContentSecurityPolicy = Politique(nonce, developpement);
                h.XContentTypeOptions = "nosniff";
                h.XFrameOptions = "DENY";
                h["Referrer-Policy"] = "strict-origin-when-cross-origin";
                // Position autorisée pour le site lui-même (GPS de l'admin sur le terrain), le reste coupé.
                h["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=(), payment=(), usb=()";
                h["Cross-Origin-Opener-Policy"] = "same-origin";
                return Task.CompletedTask;
            });
            return suivant(contexte);
        });
}
