using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Yakkomom.Helpers;

public readonly record struct PointGeo(double Lat, double Lng);

/// <summary>Contour de parcelle : lecture/validation du GeoJSON et calcul de surface.</summary>
public static class Geo
{
    /// <summary>Rayon équatorial WGS 84 (même valeur que Leaflet et Leaflet.draw).</summary>
    private const double RayonTerre = 6378137.0;

    // Emprise large du Sénégal (avec marge)
    public const double LatMin = 12.0, LatMax = 17.0, LngMin = -18.0, LngMax = -11.0;

    public const int SommetsMax = 500;

    public static bool DansLeSenegal(double lat, double lng) => lat is >= LatMin and <= LatMax && lng is >= LngMin and <= LngMax;

    /// <summary>
    /// Surface d'un polygone sur la sphère (m²), formule utilisée par Leaflet.draw (geodesicArea).
    /// Précision largement suffisante à l'échelle d'une parcelle ; résultat « à titre indicatif ».
    /// </summary>
    public static double AireM2(IReadOnlyList<PointGeo> points)
    {
        if (points.Count < 3) return 0;
        double aire = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];
            aire += Radians(p2.Lng - p1.Lng) * (2 + Math.Sin(Radians(p1.Lat)) + Math.Sin(Radians(p2.Lat)));
        }
        return Math.Abs(aire * RayonTerre * RayonTerre / 2.0);
    }

    /// <summary>Centre du polygone (moyenne des sommets : suffisant pour placer un repère).</summary>
    public static PointGeo Centre(IReadOnlyList<PointGeo> points) =>
        new(points.Average(p => p.Lat), points.Average(p => p.Lng));

    /// <summary>
    /// Lit un GeoJSON (Polygon ou Feature contenant un Polygon), anneau extérieur uniquement.
    /// Coordonnées GeoJSON = [longitude, latitude].
    /// </summary>
    public static bool LirePolygone(string? geojson, out List<PointGeo> points, out string? erreur)
    {
        points = [];
        erreur = null;
        if (string.IsNullOrWhiteSpace(geojson)) return true;
        if (geojson.Length > 100_000) { erreur = "Contour trop complexe."; return false; }

        try
        {
            using var doc = JsonDocument.Parse(geojson);
            var geometrie = doc.RootElement;
            if (geometrie.TryGetProperty("type", out var t) && t.GetString() == "Feature")
                geometrie = geometrie.GetProperty("geometry");
            if (!geometrie.TryGetProperty("type", out t) || t.GetString() != "Polygon")
            {
                erreur = "Le contour doit être un polygone.";
                return false;
            }

            var anneau = geometrie.GetProperty("coordinates")[0];
            foreach (var c in anneau.EnumerateArray())
            {
                var lng = c[0].GetDouble();
                var lat = c[1].GetDouble();
                if (!double.IsFinite(lat) || !double.IsFinite(lng) || !DansLeSenegal(lat, lng))
                {
                    erreur = "Un sommet du contour est en dehors du Sénégal.";
                    return false;
                }
                points.Add(new PointGeo(lat, lng));
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException or FormatException)
        {
            erreur = "Contour illisible.";
            points = [];
            return false;
        }

        // L'anneau GeoJSON est fermé (dernier point = premier) : on retire le doublon.
        if (points.Count > 1 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);

        if (points.Count < 3) { erreur = "Le contour doit avoir au moins 3 sommets."; return false; }
        if (points.Count > SommetsMax) { erreur = $"Le contour ne peut pas dépasser {SommetsMax} sommets."; return false; }
        var aire = AireM2(points);
        if (aire < 10) { erreur = "Contour trop petit (moins de 10 m²) : vérifiez les sommets."; return false; }
        if (aire > 100_000_000) { erreur = "Contour trop grand (plus de 10 000 ha)."; return false; }
        return true;
    }

    /// <summary>GeoJSON normalisé (7 décimales ≈ 1 cm), anneau fermé.</summary>
    public static string VersGeoJson(IReadOnlyList<PointGeo> points)
    {
        var sb = new StringBuilder("{\"type\":\"Polygon\",\"coordinates\":[[");
        foreach (var p in points.Append(points[0]))
            sb.Append('[').Append(Arrondi(p.Lng)).Append(',').Append(Arrondi(p.Lat)).Append("],");
        sb.Length--; // dernière virgule
        return sb.Append("]]}").ToString();
    }

    private static string Arrondi(double v) => Math.Round(v, 7).ToString(CultureInfo.InvariantCulture);
    private static double Radians(double degres) => degres * Math.PI / 180.0;
}
