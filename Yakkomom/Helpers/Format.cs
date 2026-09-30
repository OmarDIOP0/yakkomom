using System.Globalization;
using Yakkomom.Models.Enums;

namespace Yakkomom.Helpers;

/// <summary>Formats d'affichage sénégalais : « 12 500 000 FCFA », « 500 m² », « 1,5 ha ».</summary>
public static class Format
{
    /// <summary>Espace insécable : les montants ne sont jamais coupés en fin de ligne.</summary>
    public const char Insecable = ' ';

    private static readonly NumberFormatInfo Nombres = new()
    {
        NumberGroupSeparator = Insecable.ToString(),
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3]
    };

    public static readonly TimeZoneInfo FuseauDakar = TrouverFuseau();

    public static string Nombre(decimal valeur, int decimales = 0) =>
        valeur.ToString("N" + decimales, Nombres);

    /// <summary>12500000 → « 12 500 000 FCFA ».</summary>
    public static string Fcfa(long? montant) =>
        montant is null ? "Prix sur demande" : $"{Nombre(montant.Value)}{Insecable}FCFA";

    /// <summary>Montant seul, sans devise (pour les blocs où « FCFA » est stylé à part).</summary>
    public static string Montant(long montant) => Nombre(montant);

    /// <summary>Surface lisible : m² jusqu'à 1 ha, puis hectares.</summary>
    public static string Surface(decimal? m2)
    {
        if (m2 is null) return "—";
        if (m2 >= 10_000)
        {
            var ha = m2.Value / 10_000m;
            var texte = ha % 1 == 0 ? Nombre(ha) : Nombre(ha, 2).TrimEnd('0').TrimEnd(',');
            return $"{texte}{Insecable}ha";
        }
        return $"{Nombre(m2.Value, m2 % 1 == 0 ? 0 : 1)}{Insecable}m²";
    }

    public static long? PrixAuM2(long? prix, decimal? surfaceM2) =>
        prix is > 0 && surfaceM2 is > 0 ? (long)Math.Round(prix.Value / surfaceM2.Value) : null;

    public static string Dimensions(decimal? longueur, decimal? largeur) =>
        longueur is > 0 && largeur is > 0
            ? $"{Nombre(longueur.Value, longueur % 1 == 0 ? 0 : 1)}{Insecable}×{Insecable}{Nombre(largeur.Value, largeur % 1 == 0 ? 0 : 1)}{Insecable}m"
            : "—";

    private static readonly string[] Mois =
        ["janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc."];

    /// <summary>Heure de Dakar (UTC+0 toute l'année).</summary>
    public static DateTime HeureDakar(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FuseauDakar);

    /// <summary>« 30 sept. 2026 » (sans dépendre de la culture installée sur le serveur).</summary>
    public static string Date(DateTime utc)
    {
        var d = HeureDakar(utc);
        return $"{d.Day} {Mois[d.Month - 1]} {d.Year}";
    }

    /// <summary>« 30 sept. 2026 à 14:05 ».</summary>
    public static string DateHeure(DateTime utc)
    {
        var d = HeureDakar(utc);
        return $"{Date(utc)} à {d.Hour:00}:{d.Minute:00}";
    }

    /// <summary>« 14:05 » (heure de Dakar).</summary>
    public static string Heure(DateTime utc)
    {
        var d = HeureDakar(utc);
        return $"{d.Hour:00}:{d.Minute:00}";
    }

    /// <summary>« mercredi 30 sept. 2026 ».</summary>
    public static string JourComplet(DateOnly d) => $"{JoursSemaine[(int)d.DayOfWeek]} {d.Day} {Mois[d.Month - 1]} {d.Year}";

    /// <summary>« 30 sept. » (axes de graphiques).</summary>
    public static string JourCourt(DateOnly d) => $"{d.Day} {Mois[d.Month - 1]}";

    private static readonly string[] JoursSemaine = ["dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi"];

    public static string Statut(StatutTerrain statut) => statut switch
    {
        StatutTerrain.Brouillon => "Brouillon",
        StatutTerrain.Disponible => "Disponible",
        StatutTerrain.Reserve => "Réservé",
        StatutTerrain.Vendu => "Vendu",
        StatutTerrain.Archive => "Archivé",
        _ => statut.ToString()
    };

    private static TimeZoneInfo TrouverFuseau()
    {
        // Dakar = UTC+0 toute l'année ; repli si la base de fuseaux est absente (conteneur minimal).
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Dakar"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}
