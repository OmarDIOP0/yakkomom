using System.Globalization;
using System.Text;

namespace Yakkomom.Helpers;

public static class SlugHelper
{
    /// <summary>« Nguékokh » → « nguekokh », « M'bour » → « m-bour », « 500 m² » → « 500-m2 ».</summary>
    public static string Slugifier(string? texte, int longueurMax = 80)
    {
        if (string.IsNullOrWhiteSpace(texte)) return string.Empty;

        var normalise = texte.Replace("²", "2").Replace("œ", "oe").Replace("Œ", "oe")
            .Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalise.Length);
        var tiretPrecedent = true; // évite un tiret en tête

        foreach (var c in normalise)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                tiretPrecedent = false;
            }
            else if (!tiretPrecedent)
            {
                sb.Append('-');
                tiretPrecedent = true;
            }
        }

        var slug = sb.ToString().TrimEnd('-');
        return slug.Length <= longueurMax ? slug : slug[..longueurMax].TrimEnd('-');
    }
}
