using Yakkomom.Models.Entities;
using Yakkomom.Models.ViewModels;
using Yakkomom.Stockage;

namespace Yakkomom.Services;

/// <summary>
/// Construit src / srcset des photos. Stockage local : vignette 480 px + original 1600 px ;
/// Cloudinary : 480, 960 et 1600 px générés à la volée en WebP/AVIF selon le navigateur.
/// </summary>
public class ImagesPubliques(IStorageService stockage, IHttpContextAccessor http)
{
    public PhotoPubliqueVm Photo(TerrainPhoto p) =>
        Photo(p.CleStockage, p.CleVignette, p.Largeur, p.Hauteur, p.CouleurDominante, p.Legende);

    public PhotoPubliqueVm Photo(ServicePhoto p) =>
        Photo(p.CleStockage, p.CleVignette, p.Largeur, p.Hauteur, p.CouleurDominante, p.Legende);

    public PhotoPubliqueVm Photo(string cleStockage, string? cleVignette, int largeur, int hauteur, string? couleur, string? legende)
    {
        var p = (CleStockage: cleStockage, CleVignette: cleVignette, Largeur: largeur, Hauteur: hauteur, CouleurDominante: couleur, Legende: legende);

        // Mode économie de données : seulement la vignette (~20-40 Ko), jamais la grande image.
        if (Helpers.ModeEconomie.EstActif(http.HttpContext))
        {
            var petite = stockage.RedimensionneALaVolee
                ? stockage.UrlImage(cleStockage, 480)
                : stockage.UrlImage(cleVignette ?? cleStockage);
            return new PhotoPubliqueVm(petite, null, stockage.UrlImage(cleStockage, 1600), largeur, hauteur, couleur, legende);
        }

        string src, srcset;
        if (stockage.RedimensionneALaVolee)
        {
            src = stockage.UrlImage(p.CleStockage, 960);
            srcset = string.Join(", ", new[] { 480, 960, 1600 }
                .Where(l => l <= p.Largeur || l == 480)
                .Select(l => $"{stockage.UrlImage(p.CleStockage, l)} {Math.Min(l, p.Largeur)}w"));
        }
        else
        {
            // src de repli = vignette (navigateurs sans srcset) ; srcset propose la grande image aux écrans larges
            src = p.CleVignette is null ? stockage.UrlImage(p.CleStockage) : stockage.UrlImage(p.CleVignette);
            srcset = p.CleVignette is null
                ? $"{src} {p.Largeur}w"
                : $"{src} 480w, {stockage.UrlImage(p.CleStockage)} {p.Largeur}w";
        }
        return new PhotoPubliqueVm(src, srcset, stockage.UrlImage(p.CleStockage, 1600), p.Largeur, p.Hauteur, p.CouleurDominante, p.Legende);
    }

    /// <summary>Image pour les aperçus de partage (WhatsApp, Facebook) : ~1200 px, JPEG si possible.</summary>
    public string UrlPartage(TerrainPhoto p) => stockage.UrlImage(p.CleStockage, 1200);
}
