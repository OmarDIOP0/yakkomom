using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Yakkomom.Models.Entities;

namespace Yakkomom.Stockage;

/// <summary>Réponse HTTP pour un document foncier : flux local ou redirection vers un lien signé temporaire.</summary>
public static class ServirDocument
{
    public static IActionResult Reponse(ControllerBase controleur, DocumentFoncier document, FichierPrive fichier)
    {
        var entetes = controleur.Response.Headers;
        // Jamais mis en cache (ni par le navigateur, ni par un proxy, ni par le service worker).
        entetes.CacheControl = "private, no-store, max-age=0";
        entetes.XContentTypeOptions = "nosniff";
        entetes["X-Robots-Tag"] = "noindex, nofollow";

        switch (fichier)
        {
            case FichierPrive.Flux flux:
                var disposition = new ContentDispositionHeaderValue("inline");
                disposition.SetHttpFileName(document.NomFichierOriginal);
                entetes.ContentDisposition = disposition.ToString();
                return controleur.File(flux.Contenu, document.TypeMime, enableRangeProcessing: true);

            case FichierPrive.LienTemporaire lien:
                return controleur.Redirect(lien.Url);

            default:
                return controleur.NotFound();
        }
    }
}
