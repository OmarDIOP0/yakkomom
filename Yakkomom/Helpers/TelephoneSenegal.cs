using System.Text;

namespace Yakkomom.Helpers;

/// <summary>
/// Normalisation des numéros au format international E.164 (+221XXXXXXXXX).
/// Accepte les saisies courantes : « 77 123 45 67 », « 771234567 », « +221 77… »,
/// « 00221 77… », « 221 77… », ainsi que les numéros étrangers (diaspora) commençant par + ou 00.
/// </summary>
public static class TelephoneSenegal
{
    public const string Indicatif = "221";

    /// <summary>Retourne le numéro normalisé, ou null avec un message d'erreur.</summary>
    public static bool Normaliser(string? saisie, out string? normalise, out string? erreur)
    {
        normalise = null;
        erreur = null;
        if (string.IsNullOrWhiteSpace(saisie)) return true; // champ facultatif

        var brut = saisie.Trim();
        var international = brut.StartsWith('+') || brut.StartsWith("00");
        var chiffres = new StringBuilder();
        foreach (var c in brut)
        {
            if (char.IsAsciiDigit(c)) chiffres.Append(c);
            else if (c is not (' ' or '.' or '-' or '(' or ')' or '+' or ' '))
            {
                erreur = "Le numéro ne doit contenir que des chiffres.";
                return false;
            }
        }
        var n = chiffres.ToString();
        if (brut.StartsWith("00")) n = n[2..];

        // Numéro sénégalais sans indicatif
        if (!international && !(n.StartsWith(Indicatif) && n.Length == 12))
        {
            if (n.Length != 9)
            {
                erreur = "Numéro sénégalais incomplet : 9 chiffres attendus (ex. 77 123 45 67).";
                return false;
            }
            n = Indicatif + n;
        }

        if (n.StartsWith(Indicatif))
        {
            var local = n[Indicatif.Length..];
            if (local.Length != 9)
            {
                erreur = "Un numéro sénégalais comporte 9 chiffres après +221.";
                return false;
            }
            if (!(local[0] == '7' || local.StartsWith("33")))
            {
                erreur = "Un numéro sénégalais commence par 7 (mobile) ou 33 (fixe).";
                return false;
            }
        }
        else if (n.Length is < 8 or > 15)
        {
            erreur = "Numéro international invalide.";
            return false;
        }

        normalise = "+" + n;
        return true;
    }

    /// <summary>Chiffres seuls pour wa.me : « +221771234567 » → « 221771234567 ».</summary>
    public static string PourWhatsApp(string numeroNormalise) => numeroNormalise.TrimStart('+');

    /// <summary>Affichage lisible : « +221 77 123 45 67 ».</summary>
    public static string Afficher(string? numeroNormalise)
    {
        if (string.IsNullOrEmpty(numeroNormalise)) return string.Empty;
        var n = numeroNormalise.TrimStart('+');
        if (n.StartsWith(Indicatif) && n.Length == 12)
            return $"+221 {n[3..5]} {n[5..8]} {n[8..10]} {n[10..12]}";
        return "+" + n;
    }
}
