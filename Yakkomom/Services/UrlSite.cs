namespace Yakkomom.Services;

/// <summary>
/// URLs absolues (liens WhatsApp, partages, balises Open Graph).
/// En production, <c>Site__UrlPublique</c> (ex. https://yakkomom.sn) fixe le domaine canonique ;
/// sinon on reprend le schéma et l'hôte de la requête (derrière Render : en-têtes transférés).
/// </summary>
public class UrlSite(IHttpContextAccessor http, IConfiguration configuration)
{
    public string Base
    {
        get
        {
            var configuree = configuration["Site:UrlPublique"];
            if (!string.IsNullOrWhiteSpace(configuree)) return configuree.TrimEnd('/');
            var requete = http.HttpContext?.Request;
            return requete is null ? "" : $"{requete.Scheme}://{requete.Host}";
        }
    }

    public string Absolue(string chemin) =>
        chemin.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? chemin : Base + "/" + chemin.TrimStart('/');
}
