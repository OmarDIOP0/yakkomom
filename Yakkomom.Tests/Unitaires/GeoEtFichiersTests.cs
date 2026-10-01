using System.Text;
using System.Text.Json;
using Yakkomom.Helpers;
using Yakkomom.Securite;
using Yakkomom.Stockage;

namespace Yakkomom.Tests.Unitaires;

public class GeoTests
{
    /// <summary>Rectangle de 20 m (est-ouest) × 25 m (nord-sud) autour de Popenguine.</summary>
    private static List<PointGeo> Rectangle500m2()
    {
        const double lat = 14.5655, lng = -17.1030, m = 111_320;
        double dLat = 12.5 / m, dLng = 10 / (m * Math.Cos(lat * Math.PI / 180));
        return [new(lat + dLat, lng - dLng), new(lat + dLat, lng + dLng), new(lat - dLat, lng + dLng), new(lat - dLat, lng - dLng)];
    }

    [Fact]
    public void Surface_d_une_parcelle_de_500_m2_a_1_pourcent_pres()
    {
        Assert.InRange(Geo.AireM2(Rectangle500m2()), 495, 505);
    }

    [Fact]
    public void Contour_relu_apres_ecriture_geojson()
    {
        var json = Geo.VersGeoJson(Rectangle500m2());
        Assert.True(Geo.LirePolygone(json, out var points, out var erreur), erreur);
        Assert.Equal(4, points.Count); // anneau fermé : le point de fermeture est retiré
    }

    [Theory]
    [InlineData("{\"type\":\"Point\",\"coordinates\":[-17.1,14.5]}", "polygone")]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[2.35,48.85],[2.36,48.85],[2.36,48.86],[2.35,48.85]]]}", "en dehors du Sénégal")]
    [InlineData("{\"type\":\"Polygon\",\"coordinates\":[[[-17.1,14.5],[-17.1001,14.5],[-17.1,14.5]]]}", "au moins 3 sommets")]
    [InlineData("pas du json", "illisible")]
    public void Contour_invalide_refuse_avec_explication(string geojson, string extrait)
    {
        Assert.False(Geo.LirePolygone(geojson, out _, out var erreur));
        Assert.Contains(extrait, erreur);
    }
}

/// <summary>Les envois sont identifiés par leur contenu réel (signature binaire), jamais par leur nom ou leur type déclaré.</summary>
public class AnalyseFichierTests
{
    private static byte[] Completer(byte[] debut, int taille = 64) => [.. debut, .. new byte[Math.Max(0, taille - debut.Length)]];

    [Fact]
    public void Pdf_reconnu()
    {
        var i = AnalyseFichier.Analyser(Completer("%PDF-1.7\n"u8.ToArray()));
        Assert.Equal(("application/pdf", ".pdf"), (i?.TypeMime, i?.Extension));
    }

    [Fact]
    public void Png_reconnu_avec_dimensions()
    {
        byte[] entete = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R', 0, 0, 0x03, 0x20, 0, 0, 0x02, 0x58];
        var i = AnalyseFichier.Analyser(Completer(entete));
        Assert.Equal(("image/png", 800, 600), (i?.TypeMime, i?.Largeur, i?.Hauteur));
    }

    [Fact]
    public void Jpeg_reconnu_avec_dimensions()
    {
        byte[] entete = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01, 0xE0, 0x02, 0x80, 0x03];
        var i = AnalyseFichier.Analyser(Completer(entete));
        Assert.Equal(("image/jpeg", 640, 480), (i?.TypeMime, i?.Largeur, i?.Hauteur));
    }

    [Fact]
    public void Vraie_photo_webp_de_demonstration()
    {
        var chemin = Path.Combine(AppContext.BaseDirectory, "Demo", "photos", "popenguine-1.webp");
        var i = AnalyseFichier.Analyser(File.ReadAllBytes(chemin).AsSpan(0, AnalyseFichier.TailleEntete));
        Assert.Equal(("image/webp", 1600, 1205), (i?.TypeMime, i?.Largeur, i?.Hauteur));
    }

    [Theory]
    [InlineData("<html><script>alert(1)</script></html>")]  // page HTML déguisée
    [InlineData("MZ\u0090\0executable")]                    // exécutable Windows
    [InlineData("court")]
    public void Contenu_non_autorise_refuse(string contenu)
    {
        Assert.Null(AnalyseFichier.Analyser(Completer(Encoding.Latin1.GetBytes(contenu), contenu.Length < 12 ? 0 : 64)));
    }
}

public class EnTetesSecuriteTests
{
    [Fact]
    public void Csp_stricte_pour_les_scripts()
    {
        var csp = EnTetesSecurite.Politique("abc123", developpement: false);
        Assert.Contains("script-src 'self' 'nonce-abc123'", csp);
        Assert.DoesNotContain("unsafe-eval", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("upgrade-insecure-requests", csp);
    }

    [Fact]
    public void Json_injecte_dans_la_page_ne_peut_pas_fermer_la_balise_script()
    {
        // Même encodeur que les données structurées et la configuration de la visite 360°
        var json = JsonSerializer.Serialize(new { titre = "</script><script>alert(1)</script>" });
        Assert.DoesNotContain("</script>", json);
        Assert.DoesNotContain("<", json);
    }
}
