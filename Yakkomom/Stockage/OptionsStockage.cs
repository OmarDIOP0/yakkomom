namespace Yakkomom.Stockage;

public class OptionsStockage
{
    public const string Section = "Stockage";

    /// <summary>« Local » (développement) ou « Cloudinary » (production).</summary>
    public string Fournisseur { get; set; } = "Local";

    /// <summary>Dossier racine du stockage local (relatif au dossier de l'application).</summary>
    public string DossierLocal { get; set; } = "App_Data";

    /// <summary>Préfixe des clés côté Cloudinary (un dossier par environnement).</summary>
    public string DossierCloudinary { get; set; } = "yakkomom";
}
