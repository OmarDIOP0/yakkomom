namespace Yakkomom.Helpers;

/// <summary>Noms lisibles des champs modifiés, pour le journal des actions.</summary>
public static class LibellesChamps
{
    private static readonly Dictionary<string, string> Libelles = new(StringComparer.Ordinal)
    {
        ["Titre"] = "titre", ["Description"] = "description", ["Type"] = "type", ["Statut"] = "statut",
        ["Prix"] = "prix", ["PrixNegociable"] = "prix négociable",
        ["SurfaceM2"] = "surface", ["LongueurM"] = "longueur", ["LargeurM"] = "largeur",
        ["SituationFonciere"] = "situation foncière",
        ["RegionId"] = "région", ["DepartementId"] = "département", ["CommuneId"] = "commune",
        ["QuartierVillage"] = "quartier ou village", ["Adresse"] = "adresse",
        ["Latitude"] = "position GPS", ["Longitude"] = "position GPS",
        ["ContourGeoJson"] = "contour de la parcelle", ["SurfaceCalculeeM2"] = "surface calculée",
        ["AccesEau"] = "eau", ["AccesElectricite"] = "électricité", ["RouteAcces"] = "route d'accès",
        ["RouteAccesDetail"] = "détail de l'accès", ["Commodites"] = "commodités",
        ["VideoYoutubeUrl"] = "vidéo YouTube",
        ["EstMisEnAvant"] = "mise à la une", ["OrdreMiseEnAvant"] = "ordre de mise à la une",
        ["WhatsApp1"] = "WhatsApp 1", ["WhatsApp1Libelle"] = "libellé WhatsApp 1",
        ["WhatsApp2"] = "WhatsApp 2", ["WhatsApp2Libelle"] = "libellé WhatsApp 2",
        ["WhatsApp3"] = "WhatsApp 3", ["WhatsApp3Libelle"] = "libellé WhatsApp 3",
        ["Email"] = "e-mail",
    };

    /// <summary>« Prix, SurfaceM2, Latitude, Longitude » → « prix, surface, position GPS ».</summary>
    public static string Lister(IEnumerable<string> champs) =>
        string.Join(", ", champs.Select(c => Libelles.GetValueOrDefault(c, c)).Distinct());
}
