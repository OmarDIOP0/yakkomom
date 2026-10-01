using Yakkomom.Controllers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Services;

namespace Yakkomom.Tests.Unitaires;

public class WhatsAppTests
{
    private const string Modele = "Bonjour {site}, je suis intéressé(e) par le terrain {reference} ({resume}) : {lien}. Est-il toujours disponible ?";

    [Fact]
    public void Message_terrain_contient_reference_resume_lien_et_nom_du_site()
    {
        var m = WhatsAppLiens.Message(MotifWhatsApp.Terrain, "Yakkomom", Modele, "YK-0042", "500 m² à Nguékokh", "https://yakkomom.sn/terrains/yk-0042");
        Assert.Equal("Bonjour Yakkomom, je suis intéressé(e) par le terrain YK-0042 (500 m² à Nguékokh) : https://yakkomom.sn/terrains/yk-0042. Est-il toujours disponible ?", m);
    }

    [Fact]
    public void Message_documents_et_general_utilisent_le_nom_du_site()
    {
        Assert.StartsWith("Bonjour Teranga, je souhaite consulter les documents fonciers du terrain YK-0001",
            WhatsAppLiens.Message(MotifWhatsApp.Document, "Teranga", Modele, "YK-0001", "300 m²", "https://x"));
        Assert.StartsWith("Bonjour Teranga,", WhatsAppLiens.Message(MotifWhatsApp.General, "Teranga", Modele, "", "", ""));
    }

    [Fact]
    public void Lien_wame_encode_le_message_et_retire_le_plus()
    {
        var url = WhatsAppLiens.UrlWhatsApp("+221771234567", "Bonjour, terrain YK-0001 & 500 m² ?");
        Assert.Equal("https://wa.me/221771234567?text=Bonjour%2C%20terrain%20YK-0001%20%26%20500%20m%C2%B2%20%3F", url);
    }

    [Fact]
    public void Numeros_du_terrain_prioritaires_sinon_ceux_du_site()
    {
        var site = new Contact { WhatsApp1 = "+221770000001", WhatsApp2 = "+221770000002" };
        var terrain = new Contact { WhatsApp3 = "+221770000009", WhatsApp3Libelle = "Awa" };

        var propres = WhatsAppLiens.NumerosEffectifs(terrain, site);
        Assert.Single(propres);
        Assert.Equal((3, "+221770000009", "Awa"), (propres[0].Index, propres[0].Numero, propres[0].Libelle));

        var repli = WhatsAppLiens.NumerosEffectifs(new Contact(), site);
        Assert.Equal([1, 2], repli.Select(n => n.Index));
    }

    [Fact]
    public void Resume_court_du_terrain()
    {
        Assert.Equal("500 m² à Nguékokh".Replace(' ', Helpers.Format.Insecable), WhatsAppLiens.Resume(500, "Nguékokh"));
        Assert.Equal("terrain", WhatsAppLiens.Resume(null, null));
    }

    [Fact]
    public void Empreinte_anti_doublon_stable_et_sans_adresse_ip_en_clair()
    {
        var a = WhatsAppController.EmpreinteClic("196.207.1.2", 1, null, 1);
        Assert.Equal(a, WhatsAppController.EmpreinteClic("196.207.1.2", 1, null, 1));
        Assert.NotEqual(a, WhatsAppController.EmpreinteClic("196.207.1.2", 1, null, 2)); // autre numéro
        Assert.NotEqual(a, WhatsAppController.EmpreinteClic("196.207.1.3", 1, null, 1)); // autre visiteur
        Assert.DoesNotContain("196.207", a);
    }
}

public class CompletudeTests
{
    private static EntreeCompletude Vide() => new(null, null, null, null, null, null, null, 0, null, 0, null, null, null, false);

    private static EntreeCompletude Complete() => new(TypeTerrain.Habitation, 17_500_000, 500,
        new string('x', Completude.LongueurDescriptionMinimum), 1, 14.5, -17.1, Completude.PhotosMinimum,
        TypeDocumentFoncier.TitreFoncier, 1, true, true, true, true);

    [Fact]
    public void Brouillon_avec_seulement_le_titre()
    {
        var r = Completude.Calculer(Vide());
        Assert.Equal(5, r.Pourcentage);
        Assert.False(r.EstComplete);
        Assert.Contains(r.Manquants, c => c.Cle == "prix");
    }

    [Fact]
    public void Fiche_complete_a_100_et_poids_totalisant_100()
    {
        var r = Completude.Calculer(Complete());
        Assert.Equal(100, r.Pourcentage);
        Assert.True(r.EstComplete);
        Assert.Equal(100, r.Criteres.Sum(c => c.Poids));
        Assert.Equal("Fiche complète à 100 %.", r.Resume());
    }

    [Fact]
    public void Il_faut_au_moins_3_photos()
    {
        var r = Completude.Calculer(Complete() with { NombrePhotos = Completude.PhotosMinimum - 1 });
        Assert.Equal(80, r.Pourcentage);
        Assert.False(r.OngletComplet(OngletTerrain.Photos));
        Assert.Equal("Fiche complète à 80 % : il manque des photos (au moins 3).", r.Resume());
    }

    [Fact]
    public void Un_document_suffit_sans_situation_fonciere_et_inversement()
    {
        Assert.Equal(100, Completude.Calculer(Complete() with { SituationFonciere = null }).Pourcentage);
        Assert.Equal(100, Completude.Calculer(Complete() with { NombreDocuments = 0 }).Pourcentage);
        Assert.Equal(90, Completude.Calculer(Complete() with { SituationFonciere = null, NombreDocuments = 0 }).Pourcentage);
    }

    [Fact]
    public void Resume_liste_plusieurs_manques_avec_et()
    {
        var r = Completude.Calculer(Complete() with { Prix = null, Latitude = null });
        Assert.Equal("Fiche complète à 75 % : il manque le prix et la position GPS.", r.Resume());
    }
}
