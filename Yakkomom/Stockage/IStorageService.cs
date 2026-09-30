namespace Yakkomom.Stockage;

/// <summary>Zone de stockage : publique (photos, logo) ou privée (documents fonciers).</summary>
public enum ZoneStockage
{
    /// <summary>Servie publiquement, avec cache long (les clés sont uniques et ne changent jamais).</summary>
    Publique,
    /// <summary>Jamais accessible par URL directe : uniquement via un contrôleur qui vérifie les droits.</summary>
    Privee
}

/// <summary>Fichier privé prêt à être servi : flux local ou lien temporaire signé (cloud).</summary>
public abstract record FichierPrive
{
    public sealed record Flux(Stream Contenu) : FichierPrive;
    public sealed record LienTemporaire(string Url) : FichierPrive;
}

/// <summary>
/// Stockage des fichiers. Implémentation locale en développement (disque),
/// Cloudinary en production (le disque de Render est effacé à chaque redémarrage).
/// Les clés sont de la forme « terrains/42/photos/3f2a….webp » et ne contiennent jamais
/// de nom fourni par l'utilisateur.
/// </summary>
public interface IStorageService
{
    /// <summary>Nom du fournisseur (« Local », « Cloudinary »), pour les journaux et le diagnostic.</summary>
    string Fournisseur { get; }

    Task EnregistrerAsync(Stream contenu, string cle, string typeMime, ZoneStockage zone, CancellationToken ct = default);

    Task SupprimerAsync(string cle, ZoneStockage zone, CancellationToken ct = default);

    /// <summary>URL publique d'une image, idéalement redimensionnée à <paramref name="largeur"/> px.</summary>
    string UrlImage(string cle, int? largeur = null);

    /// <summary>
    /// Image d'aperçu de partage (WhatsApp, Facebook) : idéalement JPEG 1200×630 recadré.
    /// Retourne aussi les dimensions et le type réellement servis.
    /// </summary>
    (string Url, int Largeur, int Hauteur, string Type) ImagePartage(string cle, int largeurOrigine, int hauteurOrigine, string typeOrigine);

    /// <summary>Accès à un fichier privé, après vérification des droits par l'appelant.</summary>
    Task<FichierPrive?> OuvrirPriveAsync(string cle, string typeMime, TimeSpan dureeLien, CancellationToken ct = default);

    /// <summary>Le fournisseur sait-il redimensionner les images à la volée (tailles intermédiaires) ?</summary>
    bool RedimensionneALaVolee { get; }
}
