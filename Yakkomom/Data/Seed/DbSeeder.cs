using Microsoft.EntityFrameworkCore;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;

namespace Yakkomom.Data.Seed;

/// <summary>
/// Données de départ, insérées au démarrage. Idempotent : n'ajoute que ce qui manque
/// et ne modifie jamais ce que l'admin a déjà édité.
/// </summary>
public class DbSeeder(YakkomomDbContext db, ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedLocalitesAsync(ct);
        await SeedServicesAsync(ct);
        await SeedParametresAsync(ct);
    }

    private async Task SeedLocalitesAsync(CancellationToken ct)
    {
        // Les localités existantes sont chargées (suivies) pour servir de parents aux nouvelles :
        // tout est ensuite inséré en un seul SaveChanges.
        var existantes = await db.Localites.ToListAsync(ct);
        var index = existantes.ToDictionary(l => (l.Type, l.ParentId, l.Slug));
        var ajouts = 0;

        Localite ObtenirOuCreer(string nom, TypeLocalite type, Localite? parent, int ordre)
        {
            var slug = SlugHelper.Slugifier(nom);
            if (parent is { Id: > 0 } && index.TryGetValue((type, parent.Id, slug), out var trouvee)) return trouvee;
            if (parent is null && index.TryGetValue((type, null, slug), out trouvee)) return trouvee;

            var localite = new Localite { Nom = nom, Slug = slug, Type = type, Parent = parent, Ordre = ordre };
            db.Localites.Add(localite);
            ajouts++;
            return localite;
        }

        var ordreRegion = 0;
        foreach (var (region, departements) in DonneesLocalites.Regions)
        {
            var r = ObtenirOuCreer(region, TypeLocalite.Region, null, ordreRegion++);
            var ordreDep = 0;
            foreach (var (departement, communes) in departements)
            {
                var d = ObtenirOuCreer(departement, TypeLocalite.Departement, r, ordreDep++);
                var liste = communes.Length > 0 ? communes : [departement];
                var ordreCommune = 0;
                foreach (var commune in liste)
                    ObtenirOuCreer(commune, TypeLocalite.Commune, d, ordreCommune++);
            }
        }

        if (ajouts == 0) return;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed : {Nombre} localités ajoutées.", ajouts);
    }

    private async Task SeedServicesAsync(CancellationToken ct)
    {
        var slugs = await db.Services.Select(s => s.Slug).ToListAsync(ct);
        var ordre = 0;
        var ajouts = 0;
        foreach (var s in DonneesServices.Services)
        {
            var slug = SlugHelper.Slugifier(s.Titre);
            ordre++;
            if (slugs.Contains(slug)) continue;
            db.Services.Add(new Service
            {
                Titre = s.Titre,
                Slug = slug,
                Icone = s.Icone,
                Resume = s.Resume,
                Description = s.Description,
                MessageWhatsApp = $"Bonjour Yakkomom, je souhaite en savoir plus sur votre service « {s.Titre} ».",
                Ordre = ordre
            });
            ajouts++;
        }
        if (ajouts > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seed : {Nombre} services ajoutés.", ajouts);
        }
    }

    private async Task SeedParametresAsync(CancellationToken ct)
    {
        if (await db.ParametresSite.AnyAsync(p => p.Id == ParametreSite.IdUnique, ct)) return;

        db.ParametresSite.Add(new ParametreSite
        {
            NomSite = "Yakkomom",
            Slogan = "Des terrains vérifiés, des projets bâtis en confiance.",
            Adresse = "Sénégal"
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed : paramètres du site créés.");
    }
}
