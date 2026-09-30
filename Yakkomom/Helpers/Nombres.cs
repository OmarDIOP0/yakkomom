using System.Globalization;

namespace Yakkomom.Helpers;

/// <summary>
/// Lecture tolérante des nombres saisis par l'admin, indépendante de la culture du serveur :
/// « 12 500 000 », « 12.500.000 », « 12,5 », « 12.5 », « 1 250,75 ».
/// </summary>
public static class Nombres
{
    /// <summary>Montant entier en FCFA (tous les séparateurs sont ignorés).</summary>
    public static bool LireMontant(string? saisie, out long? valeur)
    {
        valeur = null;
        if (string.IsNullOrWhiteSpace(saisie)) return true;
        var chiffres = new string(saisie.Where(char.IsAsciiDigit).ToArray());
        if (chiffres.Length == 0 || saisie.Any(c => char.IsLetter(c) && c is not ('F' or 'C' or 'A' or 'f' or 'c' or 'a'))) return false;
        if (!long.TryParse(chiffres, NumberStyles.None, CultureInfo.InvariantCulture, out var v)) return false;
        valeur = v;
        return true;
    }

    /// <summary>
    /// Décimal (surfaces, dimensions, distances) : la virgule ou le point final sert de séparateur
    /// décimal, sauf « 1.500 » ou « 1,500 » (exactement 3 chiffres après) qui s'écrit ainsi pour 1 500.
    /// </summary>
    public static bool LireDecimal(string? saisie, out decimal? valeur)
    {
        valeur = null;
        if (string.IsNullOrWhiteSpace(saisie)) return true;
        var s = saisie.Trim().Replace(" ", "").Replace(" ", "").Replace(" ", "");
        var negatif = s.StartsWith('-');
        if (negatif) s = s[1..];

        var dernierSep = s.LastIndexOfAny([',', '.']);
        string entier = s, fraction = "";
        if (dernierSep >= 0)
        {
            var apres = s[(dernierSep + 1)..];
            // « 12.500.000 » ou « 1.500 » : séparateur de milliers, pas une décimale
            var estMilliers = apres.Length == 3 && s[..dernierSep].TrimStart('0').Length > 0;
            if (!estMilliers)
            {
                entier = s[..dernierSep];
                fraction = apres;
            }
        }
        entier = entier.Replace(",", "").Replace(".", "");
        if (entier.Length == 0) entier = "0";
        if (!entier.All(char.IsAsciiDigit) || !fraction.All(char.IsAsciiDigit)) return false;

        var texte = fraction.Length > 0 ? $"{entier}.{fraction}" : entier;
        if (!decimal.TryParse(texte, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v)) return false;
        valeur = negatif ? -v : v;
        return true;
    }

    /// <summary>Coordonnée GPS : « 14.5198 » ou « 14,5198 » (jamais de séparateur de milliers).</summary>
    public static bool LireCoordonnee(string? saisie, out double? valeur)
    {
        valeur = null;
        if (string.IsNullOrWhiteSpace(saisie)) return true;
        var s = saisie.Trim().Replace(',', '.');
        if (!double.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v))
            return false;
        valeur = v;
        return true;
    }

    /// <summary>Valeur affichée dans un champ de saisie (sans séparateur de milliers pour les décimaux).</summary>
    public static string PourSaisie(decimal? v) => v?.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',') ?? "";
    public static string PourSaisie(double? v) => v?.ToString("0.######", CultureInfo.InvariantCulture) ?? "";
    public static string PourSaisie(long? v) => v is null ? "" : Format.Montant(v.Value);
}
