using System.Globalization;
using System.Text.RegularExpressions;

namespace Yakkomom.Helpers;

/// <summary>Références lisibles des terrains : YK-0001 … YK-9999, puis YK-10000…</summary>
public static partial class ReferenceTerrain
{
    public const string Prefixe = "YK-";

    public static string Formater(long numero) =>
        Prefixe + numero.ToString("D4", CultureInfo.InvariantCulture);

    /// <summary>Extrait la référence d'un slug d'URL : « yk-0042-terrain-500m2-nguekokh » → « YK-0042 ».</summary>
    public static string? DepuisSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var m = RegexReference().Match(slug);
        return m.Success ? Prefixe + m.Groups[1].Value : null;
    }

    [GeneratedRegex(@"^yk-(\d{4,})(?:-|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RegexReference();
}
