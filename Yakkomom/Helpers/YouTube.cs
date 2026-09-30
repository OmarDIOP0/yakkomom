using System.Text.RegularExpressions;

namespace Yakkomom.Helpers;

public static partial class YouTube
{
    /// <summary>
    /// Extrait l'identifiant d'une vidéo depuis les formats courants : youtube.com/watch?v=…,
    /// youtu.be/…, youtube.com/shorts/…, youtube.com/embed/…, youtube.com/live/…
    /// </summary>
    public static string? ExtraireId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var m = RegexId().Match(url.Trim());
        return m.Success ? m.Groups["id"].Value : null;
    }

    public static string UrlCanonique(string id) => $"https://www.youtube.com/watch?v={id}";
    /// <summary>Miniature légère (~15 Ko) affichée avant le chargement du lecteur.</summary>
    public static string Miniature(string id) => $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";
    /// <summary>Lecteur sans cookies publicitaires.</summary>
    public static string UrlLecteur(string id) => $"https://www.youtube-nocookie.com/embed/{id}?autoplay=1&rel=0";

    [GeneratedRegex(@"^(?:https?://)?(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:.*&)?v=|shorts/|embed/|live/)|youtu\.be/)(?<id>[A-Za-z0-9_-]{11})(?:[?&#/].*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex RegexId();
}
