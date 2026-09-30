using System.Globalization;

namespace Yakkomom.Helpers;

/// <summary>URL publique lisible : « /terrains/yk-0042-terrain-500m2-nguekokh ».</summary>
public static class UrlTerrain
{
    public static string Slug(string reference, decimal? surfaceM2, string? commune)
    {
        var morceaux = new List<string> { reference.ToLowerInvariant(), "terrain" };
        if (surfaceM2 is > 0)
        {
            morceaux.Add(surfaceM2 >= 10_000
                ? (surfaceM2.Value / 10_000m).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', '-') + "ha"
                : Math.Round(surfaceM2.Value).ToString(CultureInfo.InvariantCulture) + "m2");
        }
        if (!string.IsNullOrWhiteSpace(commune)) morceaux.Add(SlugHelper.Slugifier(commune, 40));
        return string.Join('-', morceaux);
    }

    public static string Chemin(string reference, decimal? surfaceM2, string? commune) =>
        "/terrains/" + Slug(reference, surfaceM2, commune);
}
