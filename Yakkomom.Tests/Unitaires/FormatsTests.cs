using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Stockage;

namespace Yakkomom.Tests.Unitaires;

/// <summary>Formats sénégalais, saisies et URL : ce que voient et tapent les utilisateurs.</summary>
public class FormatsTests
{
    private static readonly string E = Format.Insecable.ToString();

    [Fact]
    public void Montant_en_FCFA_avec_espaces_insecables()
    {
        Assert.Equal($"12{E}500{E}000{E}FCFA", Format.Fcfa(12_500_000));
    }

    [Theory]
    [InlineData(500, "500 m²")]
    [InlineData(10_000, "1 ha")]
    [InlineData(15_000, "1,5 ha")]
    public void Surface_en_m2_puis_hectares(int m2, string attendu)
    {
        Assert.Equal(attendu.Replace(" ", E), Format.Surface(m2));
    }

    [Fact]
    public void Prix_au_m2_arrondi_et_absent_si_donnee_manquante()
    {
        Assert.Equal(35_000, Format.PrixAuM2(17_500_000, 500));
        Assert.Null(Format.PrixAuM2(null, 500));
        Assert.Null(Format.PrixAuM2(17_500_000, 0));
    }

    [Theory]
    [InlineData("12 500 000", 12_500_000)]
    [InlineData("12.500.000 FCFA", 12_500_000)]
    [InlineData("12500000F", 12_500_000)]
    public void Saisie_de_montant_tolerante(string saisie, long attendu)
    {
        Assert.True(Nombres.LireMontant(saisie, out var v));
        Assert.Equal(attendu, v);
    }

    [Theory]
    [InlineData("douze millions")]
    [InlineData("12 M€")]
    public void Saisie_de_montant_invalide_refusee(string saisie)
    {
        Assert.False(Nombres.LireMontant(saisie, out _));
    }

    [Theory]
    [InlineData("1,5", 1.5)]
    [InlineData("12.75", 12.75)]
    [InlineData("1.500", 1500)]   // séparateur de milliers, pas une décimale
    [InlineData("1 500,5", 1500.5)]
    public void Saisie_decimale_virgule_ou_point(string saisie, double attendu)
    {
        Assert.True(Nombres.LireDecimal(saisie, out var v));
        Assert.Equal((decimal)attendu, v);
    }

    [Theory]
    [InlineData("77 123 45 67", "+221771234567")]
    [InlineData("771234567", "+221771234567")]
    [InlineData("+221 77 123 45 67", "+221771234567")]
    [InlineData("00221771234567", "+221771234567")]
    [InlineData("+33 6 12 34 56 78", "+33612345678")] // diaspora
    public void Telephone_normalise_en_E164(string saisie, string attendu)
    {
        Assert.True(TelephoneSenegal.Normaliser(saisie, out var n, out _));
        Assert.Equal(attendu, n);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("pas un numéro")]
    public void Telephone_invalide_refuse_avec_message(string saisie)
    {
        Assert.False(TelephoneSenegal.Normaliser(saisie, out _, out var erreur));
        Assert.False(string.IsNullOrWhiteSpace(erreur));
    }

    [Fact]
    public void Telephone_affichage_et_format_whatsapp()
    {
        Assert.Equal("+221 77 123 45 67", TelephoneSenegal.Afficher("+221771234567"));
        Assert.Equal("221771234567", TelephoneSenegal.PourWhatsApp("+221771234567"));
    }

    [Theory]
    [InlineData("Nguékokh", "nguekokh")]
    [InlineData("M'bour", "m-bour")]
    [InlineData("500 m²", "500-m2")]
    public void Slug_sans_accents(string texte, string attendu)
    {
        Assert.Equal(attendu, SlugHelper.Slugifier(texte));
    }

    [Fact]
    public void Reference_terrain_et_lecture_depuis_l_url()
    {
        Assert.Equal("YK-0001", ReferenceTerrain.Formater(1));
        Assert.Equal("YK-12345", ReferenceTerrain.Formater(12345));
        Assert.Equal("YK-0042", ReferenceTerrain.DepuisSlug("yk-0042-terrain-500m2-nguekokh"));
        Assert.Null(ReferenceTerrain.DepuisSlug("terrain-sans-reference"));
    }

    [Theory]
    [InlineData("YK-0001", 500, "Nguékokh", "/terrains/yk-0001-terrain-500m2-nguekokh")]
    [InlineData("YK-0021", 10000, "Mboro", "/terrains/yk-0021-terrain-1ha-mboro")]
    [InlineData("YK-0020", 500, "Popenguine-Ndayane", "/terrains/yk-0020-terrain-500m2-popenguine-ndayane")]
    public void Url_publique_lisible(string reference, int surface, string commune, string attendu)
    {
        Assert.Equal(attendu, UrlTerrain.Chemin(reference, surface, commune));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    public void Video_youtube_identifiant_extrait(string url)
    {
        Assert.Equal("dQw4w9WgXcQ", YouTube.ExtraireId(url));
    }

    [Theory]
    [InlineData("https://vimeo.com/123")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public void Video_non_youtube_ignoree(string url)
    {
        Assert.Null(YouTube.ExtraireId(url));
    }

    [Fact]
    public void Logo_texte_met_en_valeur_la_fin_du_nom_et_encode_le_html()
    {
        Assert.Equal("Yakko<em>mom</em>", LogoTexte.Html("Yakkomom").ToString());
        Assert.Equal("Teranga <em>Terrains</em>", LogoTexte.Html("Teranga Terrains").ToString());
        Assert.DoesNotContain("<script>", LogoTexte.Html("<script>x</script>").ToString());
    }

    [Fact]
    public void Champs_modifies_lisibles_dans_le_journal()
    {
        Assert.Equal("prix, surface, position GPS", LibellesChamps.Lister(["Prix", "SurfaceM2", "Latitude", "Longitude"]));
    }

    [Fact]
    public void Url_de_base_de_donnees_render_ou_neon_convertie()
    {
        var chaine = ConnexionBaseDeDonnees.DepuisUri("postgresql://moi:mot%40passe@ep-test.eu-central-1.aws.neon.tech/neondb?sslmode=require");
        Assert.Contains("Host=ep-test.eu-central-1.aws.neon.tech", chaine);
        Assert.Contains("Database=neondb", chaine);
        Assert.Contains("Username=moi", chaine);
        Assert.Contains("Password=mot@passe", chaine);
        Assert.Contains("SSL Mode=Require", chaine);
    }

    [Theory]
    [InlineData("cloudinary://1:abc@demo")]
    [InlineData("CLOUDINARY_URL=cloudinary://1:abc@demo")]
    [InlineData("  \"cloudinary://1:abc@demo\"\n")]
    public void Variable_cloudinary_tolere_les_copier_coller(string brute)
    {
        Assert.Equal("cloudinary://1:abc@demo", StockageCloudinary.NettoyerUrl(brute));
    }
}
