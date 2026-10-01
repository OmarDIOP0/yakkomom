using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Yakkomom.Stockage;

/// <summary>
/// Stockage Cloudinary (production). Configuration : variable d'environnement
/// <c>CLOUDINARY_URL=cloudinary://CLE:SECRET@NOM_DU_CLOUD</c>.
/// <list type="bullet">
/// <item>Zone publique : images « upload », redimensionnées à la volée (w_480, f_auto, q_auto).</item>
/// <item>Zone privée : fichiers « raw » de type « authenticated » : inaccessibles sans lien signé ;
/// on ne distribue que des liens de téléchargement expirant après quelques minutes.</item>
/// </list>
/// </summary>
public class StockageCloudinary : IStorageService
{
    private const string TypeRestreint = "authenticated";
    private readonly Cloudinary _cloudinary;
    private readonly string _dossier;
    private readonly ILogger<StockageCloudinary> _logger;

    public StockageCloudinary(IOptions<OptionsStockage> options, IConfiguration configuration, ILogger<StockageCloudinary> logger)
    {
        var url = NettoyerUrl(configuration["CLOUDINARY_URL"]);
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("Stockage Cloudinary choisi mais la variable CLOUDINARY_URL est absente.");
        // Message sans la valeur : elle contient le secret de l'API.
        if (!url.StartsWith("cloudinary://", StringComparison.Ordinal) || url.Contains('<'))
            throw new InvalidOperationException(
                "CLOUDINARY_URL invalide : attendu « cloudinary://cle:secret@nom-du-cloud » (copiez la ligne « API environment variable », secret affiché).");
        _cloudinary = new Cloudinary(url) { Api = { Secure = true } };
        _dossier = options.Value.DossierCloudinary.Trim('/');
        _logger = logger;
    }

    public string Fournisseur => "Cloudinary";
    public bool RedimensionneALaVolee => true;

    /// <summary>
    /// Tolère les copier-coller courants : « CLOUDINARY_URL=cloudinary://… », guillemets, espaces ou retour à la ligne.
    /// </summary>
    internal static string NettoyerUrl(string? brute)
    {
        var url = (brute ?? "").Trim().Trim('"', '\'').Trim();
        const string Prefixe = "CLOUDINARY_URL=";
        if (url.StartsWith(Prefixe, StringComparison.OrdinalIgnoreCase))
            url = url[Prefixe.Length..].Trim().Trim('"', '\'').Trim();
        return url;
    }

    public async Task EnregistrerAsync(Stream contenu, string cle, string typeMime, ZoneStockage zone, CancellationToken ct = default)
    {
        var nomFichier = Path.GetFileName(cle);
        UploadResult resultat;
        if (zone == ZoneStockage.Publique && typeMime.StartsWith("image/", StringComparison.Ordinal))
        {
            resultat = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(nomFichier, contenu),
                PublicId = IdPublic(cle, sansExtension: true),
                Overwrite = false,
                UseFilename = false,
                UniqueFilename = false
            }, ct);
        }
        else
        {
            resultat = await _cloudinary.UploadAsync(new RawUploadParams
            {
                File = new FileDescription(nomFichier, contenu),
                PublicId = IdPublic(cle, sansExtension: false),
                Type = zone == ZoneStockage.Privee ? TypeRestreint : "upload",
                Overwrite = false
            }, "raw", ct);
        }

        if (resultat.Error is not null)
            throw new IOException($"Envoi Cloudinary impossible : {resultat.Error.Message}");
    }

    public async Task SupprimerAsync(string cle, ZoneStockage zone, CancellationToken ct = default)
    {
        var estImage = zone == ZoneStockage.Publique && !cle.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var resultat = await _cloudinary.DestroyAsync(new DeletionParams(IdPublic(cle, sansExtension: estImage))
        {
            ResourceType = estImage ? ResourceType.Image : ResourceType.Raw,
            Type = zone == ZoneStockage.Privee ? TypeRestreint : "upload",
            Invalidate = true
        });
        if (resultat.Error is not null)
            _logger.LogWarning("Suppression Cloudinary de {Cle} impossible : {Erreur}", cle, resultat.Error.Message);
    }

    public string UrlImage(string cle, int? largeur = null)
    {
        var transformation = new Transformation().Quality("auto").FetchFormat("auto");
        if (largeur is > 0) transformation = transformation.Width(largeur.Value).Crop("limit");
        return _cloudinary.Api.UrlImgUp.Secure(true).Transform(transformation).BuildUrl(IdPublic(cle, sansExtension: true));
    }

    /// <summary>JPEG 1200×630 recadré sur le sujet principal (g_auto) : format le mieux reconnu par WhatsApp.</summary>
    public (string Url, int Largeur, int Hauteur, string Type) ImagePartage(string cle, int largeurOrigine, int hauteurOrigine, string typeOrigine)
    {
        var t = new Transformation().Width(1200).Height(630).Crop("fill").Gravity("auto").Quality("auto:good").FetchFormat("jpg");
        return (_cloudinary.Api.UrlImgUp.Secure(true).Transform(t).BuildUrl(IdPublic(cle, sansExtension: true)), 1200, 630, "image/jpeg");
    }

    public Task<FichierPrive?> OuvrirPriveAsync(string cle, string typeMime, TimeSpan dureeLien, CancellationToken ct = default)
    {
        var expiration = DateTimeOffset.UtcNow.Add(dureeLien).ToUnixTimeSeconds();
        var url = _cloudinary.DownloadPrivate(IdPublic(cle, sansExtension: false), attachment: false, format: "",
            type: TypeRestreint, expiresAt: expiration, resourceType: "raw");
        return Task.FromResult<FichierPrive?>(new FichierPrive.LienTemporaire(url));
    }

    /// <summary>Les images publiques sont identifiées sans extension (le format est choisi à la livraison).</summary>
    private string IdPublic(string cle, bool sansExtension)
    {
        var id = sansExtension ? Path.ChangeExtension(cle, null)! : cle;
        return $"{_dossier}/{id.Replace('\\', '/')}";
    }
}
