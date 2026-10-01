using System.Globalization;
using System.Text.RegularExpressions;

namespace Yakkomom.Helpers;

/// <summary>
/// Position GPS extraite d'un lien Google Maps (mêmes formats que le script de l'admin) :
/// « …/@14.5198,-17.0021,17z », « …!3d14.5198!4d-17.0021 », « …?q=14.5198,-17.0021 ».
/// </summary>
public static partial class LienCarte
{
    /// <summary>Domaines suivis lors de la résolution d'un lien court (aucun autre hôte n'est contacté).</summary>
    public static readonly string[] HotesAutorises =
        ["maps.app.goo.gl", "goo.gl", "maps.google.com", "www.google.com", "google.com", "www.google.sn", "google.sn", "consent.google.com"];

    public static bool EstLienCourt(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps
        && (u.Host == "maps.app.goo.gl" || (u.Host == "goo.gl" && u.AbsolutePath.StartsWith("/maps", StringComparison.Ordinal)));

    public static bool HoteAutorise(Uri u) => u.Scheme == Uri.UriSchemeHttps && HotesAutorises.Contains(u.Host, StringComparer.OrdinalIgnoreCase);

    public static (double Lat, double Lng)? Extraire(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte)) return null;
        var t = Uri.UnescapeDataString(texte);

        // Page de consentement Google : la vraie adresse est dans « continue »
        var suite = ContinueRegex().Match(t);
        if (suite.Success) t = Uri.UnescapeDataString(suite.Groups[1].Value);

        var m = PlaceRegex().Match(t);
        if (!m.Success) m = ArobaseRegex().Match(t);
        if (!m.Success) return null;
        var lat = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var lng = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        return Math.Abs(lat) <= 90 && Math.Abs(lng) <= 180 ? (lat, lng) : null;
    }

    [GeneratedRegex(@"!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)")]
    private static partial Regex PlaceRegex();

    [GeneratedRegex(@"[@=/](-?\d{1,2}\.\d+),\s*\+?(-?\d{1,3}\.\d+)")]
    private static partial Regex ArobaseRegex();

    [GeneratedRegex(@"[?&]continue=([^&]+)")]
    private static partial Regex ContinueRegex();
}
