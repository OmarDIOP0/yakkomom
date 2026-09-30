namespace Yakkomom.Models.ViewModels;

/// <summary>Image d'aperçu de partage (Open Graph / Twitter), URL absolue.</summary>
public record ImagePartage(string Url, int Largeur, int Hauteur, string Type, string Alt);

/// <summary>
/// Métadonnées d'une page : titre, description, URL canonique, aperçu de partage (WhatsApp,
/// Facebook, X), consignes aux robots et données structurées schema.org (JSON-LD).
/// Placée dans ViewData["Meta"] par les contrôleurs, rendue par le layout.
/// </summary>
public class MetaPage
{
    public const string Cle = "Meta";

    public string? Titre { get; set; }
    public string? Description { get; set; }
    public string? Canonique { get; set; }
    public ImagePartage? Image { get; set; }
    /// <summary>website, article, product…</summary>
    public string Type { get; set; } = "website";
    /// <summary>Ex. « noindex, follow » pour les pages filtrées ou personnelles.</summary>
    public string? Robots { get; set; }
    /// <summary>Blocs JSON-LD déjà sérialisés (sûrs pour l'intégration dans &lt;script&gt;).</summary>
    public List<string> DonneesStructurees { get; } = [];
    /// <summary>Informations propres aux annonces (prix) pour Open Graph.</summary>
    public long? PrixFcfa { get; set; }
}
