using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;

namespace Yakkomom.Services;

/// <summary>Onglets du formulaire terrain (l'ordre est celui de l'affichage).</summary>
/// <remarks>Lots est en dernier : « Enregistrer et continuer » s'arrête à Contacts.</remarks>
public enum OngletTerrain { Infos, Localisation, Photos, Visite360, Documents, Contacts, Lots }

/// <summary>Données nécessaires au calcul (projection légère, utilisable dans une liste).</summary>
public record EntreeCompletude(
    TypeTerrain? Type,
    long? Prix,
    decimal? SurfaceM2,
    string? Description,
    int? CommuneId,
    double? Latitude,
    double? Longitude,
    int NombrePhotos,
    TypeDocumentFoncier? SituationFonciere,
    int NombreDocuments,
    bool? AccesEau,
    bool? AccesElectricite,
    bool? RouteAcces,
    bool AUnWhatsApp)
{
    public static EntreeCompletude Depuis(Terrain t, Contact contactsParDefaut) => new(
        t.Type, t.Prix, t.SurfaceM2, t.Description, t.CommuneId, t.Latitude, t.Longitude,
        t.Photos.Count, t.SituationFonciere, t.Documents.Count,
        t.AccesEau, t.AccesElectricite, t.RouteAcces,
        ContactAvecWhatsApp(t.Contacts) || ContactAvecWhatsApp(contactsParDefaut));

    public static bool ContactAvecWhatsApp(Contact? c) =>
        c is not null && !(string.IsNullOrEmpty(c.WhatsApp1) && string.IsNullOrEmpty(c.WhatsApp2) && string.IsNullOrEmpty(c.WhatsApp3));
}

public record CritereCompletude(string Cle, string Manque, int Poids, OngletTerrain Onglet, bool Rempli);

public record ResultatCompletude(int Pourcentage, IReadOnlyList<CritereCompletude> Criteres)
{
    public IEnumerable<CritereCompletude> Manquants => Criteres.Where(c => !c.Rempli);
    public bool EstComplete => Pourcentage >= 100;
    public bool OngletComplet(OngletTerrain onglet) => Criteres.Where(c => c.Onglet == onglet).All(c => c.Rempli);
    public bool OngletAdesCriteres(OngletTerrain onglet) => Criteres.Any(c => c.Onglet == onglet);

    /// <summary>« Fiche complète à 60 % : il manque la position GPS et le document foncier. »</summary>
    public string Resume()
    {
        var manquants = Manquants.Select(c => c.Manque).ToList();
        if (manquants.Count == 0) return "Fiche complète à 100 %.";
        var liste = manquants.Count == 1
            ? manquants[0]
            : string.Join(", ", manquants.Take(manquants.Count - 1)) + " et " + manquants[^1];
        return $"Fiche complète à {Pourcentage} % : il manque {liste}.";
    }
}

/// <summary>
/// Jauge de complétude d'une fiche terrain. Les poids totalisent 100.
/// La visite 360°, la vidéo et le contour de parcelle sont des bonus : ils ne pénalisent pas.
/// </summary>
public static class Completude
{
    public const int PhotosMinimum = 3;
    public const int LongueurDescriptionMinimum = 80;

    public static ResultatCompletude Calculer(EntreeCompletude e)
    {
        CritereCompletude[] criteres =
        [
            new("titre", "le titre", 5, OngletTerrain.Infos, true), // obligatoire à la création
            new("type", "le type de terrain", 5, OngletTerrain.Infos, e.Type is not null),
            new("prix", "le prix", 15, OngletTerrain.Infos, e.Prix is > 0),
            new("surface", "la surface", 10, OngletTerrain.Infos, e.SurfaceM2 is > 0),
            new("description", "une description", 5, OngletTerrain.Infos,
                (e.Description?.Trim().Length ?? 0) >= LongueurDescriptionMinimum),
            new("viabilisation", "la viabilisation (eau, électricité, route)", 5, OngletTerrain.Infos,
                e.AccesEau is not null && e.AccesElectricite is not null && e.RouteAcces is not null),
            new("commune", "la commune", 10, OngletTerrain.Localisation, e.CommuneId is not null),
            new("gps", "la position GPS", 10, OngletTerrain.Localisation, e.Latitude is not null && e.Longitude is not null),
            new("photos", $"des photos (au moins {PhotosMinimum})", 20, OngletTerrain.Photos, e.NombrePhotos >= PhotosMinimum),
            new("foncier", "le document foncier", 10, OngletTerrain.Documents,
                e.SituationFonciere is not null || e.NombreDocuments > 0),
            new("whatsapp", "un contact WhatsApp", 5, OngletTerrain.Contacts, e.AUnWhatsApp),
        ];

        var total = criteres.Sum(c => c.Poids);
        var obtenu = criteres.Where(c => c.Rempli).Sum(c => c.Poids);
        return new ResultatCompletude((int)Math.Round(100.0 * obtenu / total), criteres);
    }
}
