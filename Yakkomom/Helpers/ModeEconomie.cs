namespace Yakkomom.Helpers;

/// <summary>
/// Mode économie de données : activé par l'en-tête Save-Data du téléphone (« économiseur de données »)
/// ou choisi par le visiteur (cookie). Effets : vignettes uniquement, pas de police web,
/// aucune carte ni aucun média chargé automatiquement.
/// </summary>
public static class ModeEconomie
{
    public const string Cookie = "yk-economie";

    public static bool EstActif(HttpContext? contexte) =>
        contexte is not null && (ParLeTelephone(contexte) || ParLeVisiteur(contexte));

    public static bool ParLeTelephone(HttpContext contexte) =>
        contexte.Request.Headers["Save-Data"].ToString().Equals("on", StringComparison.OrdinalIgnoreCase);

    public static bool ParLeVisiteur(HttpContext contexte) => contexte.Request.Cookies[Cookie] == "1";
}
