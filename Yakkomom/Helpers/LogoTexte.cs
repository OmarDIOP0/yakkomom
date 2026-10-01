using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Yakkomom.Helpers;

/// <summary>
/// Nom du site façon logo : la fin est mise en valeur (« Yakko<em>mom</em> »).
/// Plusieurs mots : le dernier ; un seul mot : les 40 % de fin.
/// </summary>
public static class LogoTexte
{
    public static IHtmlContent Html(string? nom)
    {
        nom = string.IsNullOrWhiteSpace(nom) ? "Yakkomom" : nom.Trim();
        var e = HtmlEncoder.Default;
        var espace = nom.LastIndexOf(' ');
        int coupure = espace > 0 ? espace + 1 : (int)Math.Ceiling(nom.Length * 0.6);
        if (coupure <= 0 || coupure >= nom.Length) return new HtmlString(e.Encode(nom));
        return new HtmlString($"{e.Encode(nom[..coupure])}<em>{e.Encode(nom[coupure..])}</em>");
    }
}
