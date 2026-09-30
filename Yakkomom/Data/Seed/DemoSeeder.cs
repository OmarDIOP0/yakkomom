using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Services.Interfaces;
using Yakkomom.Stockage;

namespace Yakkomom.Data.Seed;

/// <summary>
/// Terrains de démonstration (dossier <c>Demo/</c>) pour présenter le site avant d'avoir de vraies annonces.
/// Activé uniquement si <c>Demo__Terrains=true</c>. Idempotent : un terrain dont le titre existe déjà
/// n'est pas recréé, et un terrain de démo supprimé depuis l'admin n'est recréé qu'au redémarrage
/// si la variable est toujours active. Pour ne plus rien créer : retirer la variable.
/// </summary>
public class DemoSeeder(
    YakkomomDbContext db,
    IStorageService stockage,
    IJournalService journal,
    IWebHostEnvironment env,
    IConfiguration configuration,
    ILogger<DemoSeeder> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!configuration.GetValue("Demo:Terrains", false)) return;

        var dossier = Path.Combine(env.ContentRootPath, "Demo");
        var chemin = Path.Combine(dossier, "terrains.json");
        if (!File.Exists(chemin))
        {
            logger.LogWarning("Demo:Terrains est activé mais {Chemin} est introuvable.", chemin);
            return;
        }

        var donnees = JsonSerializer.Deserialize<FichierDemo>(await File.ReadAllTextAsync(chemin, ct), Json)!;
        foreach (var d in donnees.Terrains)
        {
            if (await db.Terrains.AnyAsync(t => t.Titre == d.Titre, ct)) continue;
            try
            {
                await CreerAsync(d, Path.Combine(dossier, "photos"), ct);
            }
            catch (Exception ex)
            {
                // Une démo incomplète ne doit jamais empêcher le site de démarrer.
                logger.LogError(ex, "Terrain de démo « {Titre} » non créé.", d.Titre);
            }
        }
    }

    private async Task CreerAsync(TerrainDemo d, string dossierPhotos, CancellationToken ct)
    {
        var commune = await db.Localites.AsNoTracking().Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Type == TypeLocalite.Commune && c.Nom == d.Commune, ct);

        var numero = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{YakkomomDbContext.SequenceReferenceTerrain}') AS \"Value\"")
            .SingleAsync(ct);

        var contour = Rectangle(d.Lat, d.Lng, d.Largeur, d.Longueur);
        var terrain = new Terrain
        {
            Reference = ReferenceTerrain.Formater(numero),
            Titre = d.Titre,
            Description = d.Description,
            Type = Enum.Parse<TypeTerrain>(d.Type),
            Statut = Enum.Parse<StatutTerrain>(d.Statut),
            Prix = d.Prix,
            PrixNegociable = d.Negociable,
            SurfaceM2 = d.Surface,
            LongueurM = d.Longueur,
            LargeurM = d.Largeur,
            SituationFonciere = Enum.Parse<TypeDocumentFoncier>(d.Foncier),
            CommuneId = commune?.Id,
            DepartementId = commune?.ParentId,
            RegionId = commune?.Parent?.ParentId,
            QuartierVillage = d.Quartier,
            Latitude = d.Lat,
            Longitude = d.Lng,
            ContourGeoJson = Geo.VersGeoJson(contour),
            SurfaceCalculeeM2 = Math.Round((decimal)Geo.AireM2(contour), 1),
            AccesEau = d.Eau,
            AccesElectricite = d.Electricite,
            RouteAcces = d.Route,
            RouteAccesDetail = d.RouteDetail,
            Commodites = d.Commodites.Select(c => new Commodite { Libelle = c.Libelle, DistanceKm = c.DistanceKm }).ToList(),
            EstMisEnAvant = d.MiseEnAvant,
            PublieLe = DateTime.UtcNow
        };
        db.Terrains.Add(terrain);
        await db.SaveChangesAsync(ct);

        var ordre = 0;
        foreach (var p in d.Photos)
        {
            var nom = Guid.NewGuid().ToString("N");
            var cle = $"terrains/{terrain.Id}/photos/{nom}.webp";
            await using (var f = File.OpenRead(Path.Combine(dossierPhotos, p.Fichier)))
                await stockage.EnregistrerAsync(f, cle, "image/webp", ZoneStockage.Publique, ct);

            string? cleVignette = null;
            if (!stockage.RedimensionneALaVolee && p.Vignette is not null)
            {
                cleVignette = $"terrains/{terrain.Id}/photos/{nom}-480.webp";
                await using var f = File.OpenRead(Path.Combine(dossierPhotos, p.Vignette));
                await stockage.EnregistrerAsync(f, cleVignette, "image/webp", ZoneStockage.Publique, ct);
            }

            var legende = $"{p.Legende} · {p.Credit}";
            db.TerrainPhotos.Add(new TerrainPhoto
            {
                TerrainId = terrain.Id,
                CleStockage = cle,
                CleVignette = cleVignette,
                TypeMime = "image/webp",
                Largeur = p.Largeur,
                Hauteur = p.Hauteur,
                TailleOctets = new FileInfo(Path.Combine(dossierPhotos, p.Fichier)).Length,
                CouleurDominante = p.Couleur,
                Legende = legende.Length > 200 ? legende[..200] : legende,
                Ordre = ordre,
                EstCouverture = ordre == 0
            });
            ordre++;
        }
        await db.SaveChangesAsync(ct);

        await journal.EnregistrerPourAsync(null, "Démo", TypeAction.Creation, nameof(Terrain), terrain.Id.ToString(),
            $"Création du terrain de démonstration {terrain.Reference} « {terrain.Titre} »", ct: ct);
        logger.LogInformation("Terrain de démo {Reference} créé ({Photos} photos).", terrain.Reference, ordre);
    }

    /// <summary>Contour rectangulaire (largeur est-ouest × longueur nord-sud, en mètres) centré sur le point.</summary>
    private static List<PointGeo> Rectangle(double lat, double lng, decimal largeurM, decimal longueurM)
    {
        const double MetresParDegre = 111_320;
        var dLat = (double)longueurM / 2 / MetresParDegre;
        var dLng = (double)largeurM / 2 / (MetresParDegre * Math.Cos(lat * Math.PI / 180));
        return [new(lat + dLat, lng - dLng), new(lat + dLat, lng + dLng), new(lat - dLat, lng + dLng), new(lat - dLat, lng - dLng)];
    }

    private sealed record FichierDemo(List<TerrainDemo> Terrains);

    private sealed record TerrainDemo(
        string Titre, string Type, string Statut, bool MiseEnAvant, long Prix, bool Negociable,
        decimal Surface, decimal Longueur, decimal Largeur, string Foncier, string Commune, string? Quartier,
        double Lat, double Lng, bool? Eau, bool? Electricite, bool? Route, string? RouteDetail,
        string Description, List<CommoditeDemo> Commodites, List<PhotoDemo> Photos);

    private sealed record CommoditeDemo(string Libelle, decimal? DistanceKm);

    private sealed record PhotoDemo(string Fichier, string? Vignette, int Largeur, int Hauteur, string? Couleur, string Legende, string Credit);
}
