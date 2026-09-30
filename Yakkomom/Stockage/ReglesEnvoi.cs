namespace Yakkomom.Stockage;

/// <summary>Limites des fichiers acceptés (vérifiées côté serveur ; le navigateur compresse avant l'envoi).</summary>
public static class ReglesEnvoi
{
    // Photos de terrain (déjà compressées en WebP ~1600 px par le navigateur)
    public const long PhotoTailleMax = 3 * 1024 * 1024;
    public const int PhotoDimensionMax = 2560;
    public const int PhotoDimensionMin = 200;
    public static readonly string[] PhotoTypes = ["image/webp", "image/jpeg"];

    public const long VignetteTailleMax = 250 * 1024;
    public const int VignetteDimensionMax = 800;

    // Documents fonciers (PDF tels quels, images de documents compressées)
    public const long DocumentTailleMax = 15 * 1024 * 1024;
    public static readonly string[] DocumentTypes = ["application/pdf", "image/jpeg", "image/png", "image/webp"];

    // Logo du site
    public const long LogoTailleMax = 1024 * 1024;
    public const int LogoDimensionMax = 1024;
    public static readonly string[] LogoTypes = ["image/webp", "image/png", "image/jpeg"];

    /// <summary>Taille maximale d'une requête d'envoi de document (fichier + champs).</summary>
    public const long RequeteDocumentMax = DocumentTailleMax + 64 * 1024;
    public const long RequetePhotoMax = PhotoTailleMax + VignetteTailleMax + 64 * 1024;

    public static string TailleLisible(long octets) => octets switch
    {
        >= 1024 * 1024 => $"{octets / 1024d / 1024d:0.#} Mo".Replace('.', ','),
        >= 1024 => $"{octets / 1024d:0} Ko",
        _ => $"{octets} o"
    };
}
