namespace Yakkomom.Services.Interfaces;

/// <summary>URLs des fichiers statiques avec empreinte (cache HTTP immuable).</summary>
public interface IAssetsStatiques
{
    /// <summary>Ex. <c>Assets["css/site.css"]</c> → « /css/site.xy469wgme2.css ».</summary>
    string this[string chemin] { get; }
    string Url(string chemin);

    /// <summary>
    /// Import map JSON : « /js/site.js » → « /js/site.abc123.js » pour tous les modules.
    /// Les imports relatifs entre modules pointent ainsi vers l'URL à empreinte (une seule instance, cache immuable).
    /// </summary>
    /// <param name="inclureAdmin">Inclure les modules js/admin/ (inutiles sur les pages publiques).</param>
    string ImportMap(bool inclureAdmin = false);
}
