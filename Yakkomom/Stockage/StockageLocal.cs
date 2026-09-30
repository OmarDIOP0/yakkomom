using Microsoft.Extensions.Options;

namespace Yakkomom.Stockage;

/// <summary>
/// Stockage sur disque, pour le développement.
/// <list type="bullet">
/// <item>Zone publique : <c>App_Data/media</c>, servie sous <c>/media</c> (voir Program.cs).</item>
/// <item>Zone privée : <c>App_Data/prive</c>, jamais exposée : lue uniquement par <see cref="OuvrirPriveAsync"/>.</item>
/// </list>
/// </summary>
public class StockageLocal : IStorageService
{
    public const string CheminPublic = "/media";

    private readonly string _racinePublique;
    private readonly string _racinePrivee;

    public StockageLocal(IOptions<OptionsStockage> options, IWebHostEnvironment env)
    {
        var racine = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.DossierLocal));
        _racinePublique = Path.Combine(racine, "media");
        _racinePrivee = Path.Combine(racine, "prive");
        Directory.CreateDirectory(_racinePublique);
        Directory.CreateDirectory(_racinePrivee);
    }

    public string Fournisseur => "Local";
    public bool RedimensionneALaVolee => false;
    public string RacinePublique => _racinePublique;

    public async Task EnregistrerAsync(Stream contenu, string cle, string typeMime, ZoneStockage zone, CancellationToken ct = default)
    {
        var chemin = Chemin(cle, zone);
        Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);
        // Écriture dans un fichier temporaire puis renommage : jamais de fichier à moitié écrit.
        var temporaire = chemin + ".tmp";
        await using (var fichier = new FileStream(temporaire, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await contenu.CopyToAsync(fichier, ct);
        File.Move(temporaire, chemin, overwrite: true);
    }

    public Task SupprimerAsync(string cle, ZoneStockage zone, CancellationToken ct = default)
    {
        var chemin = Chemin(cle, zone);
        if (File.Exists(chemin)) File.Delete(chemin);
        return Task.CompletedTask;
    }

    public string UrlImage(string cle, int? largeur = null) => $"{CheminPublic}/{cle}";

    // Pas de conversion possible en local : l'image d'origine (WebP) est utilisée telle quelle.
    public (string Url, int Largeur, int Hauteur, string Type) ImagePartage(string cle, int largeurOrigine, int hauteurOrigine, string typeOrigine) =>
        (UrlImage(cle), largeurOrigine, hauteurOrigine, typeOrigine);

    public Task<FichierPrive?> OuvrirPriveAsync(string cle, string typeMime, TimeSpan dureeLien, CancellationToken ct = default)
    {
        var chemin = Chemin(cle, ZoneStockage.Privee);
        FichierPrive? resultat = File.Exists(chemin)
            ? new FichierPrive.Flux(new FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            : null;
        return Task.FromResult(resultat);
    }

    /// <summary>Chemin physique, en refusant toute clé qui sortirait de la racine (« ../ »).</summary>
    private string Chemin(string cle, ZoneStockage zone)
    {
        var racine = zone == ZoneStockage.Publique ? _racinePublique : _racinePrivee;
        var chemin = Path.GetFullPath(Path.Combine(racine, cle.Replace('/', Path.DirectorySeparatorChar)));
        if (!chemin.StartsWith(racine + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Clé de stockage invalide.");
        return chemin;
    }
}
