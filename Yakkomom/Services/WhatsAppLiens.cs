using Yakkomom.Helpers;
using Yakkomom.Models.Entities;

namespace Yakkomom.Services;

public enum MotifWhatsApp { Terrain, Document, General }

/// <summary>
/// Liens WhatsApp : https://wa.me/221XXXXXXXXX?text=… avec un message pré-rempli contenant
/// la référence et le lien du terrain (modèle réglable dans les paramètres du site).
/// </summary>
public static class WhatsAppLiens
{
    public record NumeroWhatsApp(int Index, string Numero, string? Libelle);

    /// <summary>Numéros du terrain s'il en a au moins un, sinon ceux des paramètres du site.</summary>
    public static IReadOnlyList<NumeroWhatsApp> NumerosEffectifs(Contact? terrain, Contact defaut)
    {
        var source = Services.EntreeCompletude.ContactAvecWhatsApp(terrain) ? terrain! : defaut;
        return new[] { (1, source.WhatsApp1, source.WhatsApp1Libelle), (2, source.WhatsApp2, source.WhatsApp2Libelle), (3, source.WhatsApp3, source.WhatsApp3Libelle) }
            .Where(x => !string.IsNullOrEmpty(x.Item2))
            .Select(x => new NumeroWhatsApp(x.Item1, x.Item2!, x.Item3))
            .ToList();
    }

    public static string? EmailEffectif(Contact? terrain, Contact defaut) =>
        !string.IsNullOrEmpty(terrain?.Email) ? terrain.Email : defaut.Email;

    /// <summary>« 500 m² à Nguékokh » (texte court utilisé dans les messages et les partages).</summary>
    public static string Resume(decimal? surfaceM2, string? lieu)
    {
        var morceaux = new List<string>();
        if (surfaceM2 is > 0) morceaux.Add(Format.Surface(surfaceM2));
        if (!string.IsNullOrWhiteSpace(lieu)) morceaux.Add("à " + lieu);
        return morceaux.Count > 0 ? string.Join(" ", morceaux) : "terrain";
    }

    public static string Message(MotifWhatsApp motif, string nomSite, string modeleTerrain, string reference, string resume, string lien) => motif switch
    {
        MotifWhatsApp.Document =>
            $"Bonjour {nomSite}, je souhaite consulter les documents fonciers du terrain {reference} ({resume}) : {lien}. Est-ce possible ?",
        MotifWhatsApp.General => $"Bonjour {nomSite}, je souhaite avoir des informations sur vos terrains et services.",
        _ => modeleTerrain.Replace("{reference}", reference).Replace("{resume}", resume).Replace("{lien}", lien)
    };

    public static string UrlWhatsApp(string numeroNormalise, string message) =>
        $"https://wa.me/{TelephoneSenegal.PourWhatsApp(numeroNormalise)}?text={Uri.EscapeDataString(message)}";

    /// <summary>Partage d'un lien sans destinataire (repli quand le navigateur n'a pas le partage natif).</summary>
    public static string UrlPartage(string texte) => $"https://wa.me/?text={Uri.EscapeDataString(texte)}";
}
