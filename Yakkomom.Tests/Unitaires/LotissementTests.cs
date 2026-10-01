using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels;
using Yakkomom.Services;

namespace Yakkomom.Tests.Unitaires;

public class LotissementTests
{
    private static Lot L(string n, decimal? m2, long? prixM2, long? prix = null, StatutLot s = StatutLot.Disponible) =>
        new() { Numero = n, SurfaceM2 = m2, PrixM2 = prixM2, Prix = prix, Statut = s };

    [Fact]
    public void Prix_du_lot_au_m2_ou_fixe()
    {
        Assert.Equal(4_500_000, L("1", 300, 15_000).PrixEffectif);
        Assert.Equal(6_000_000, L("2", 300, 15_000, prix: 6_000_000).PrixEffectif); // le prix fixe l'emporte
        Assert.Equal(20_000, L("3", 300, null, prix: 6_000_000).PrixM2Effectif);
        Assert.Null(L("4", null, 15_000).PrixEffectif);
    }

    [Fact]
    public void Fourchettes_calculees_sur_les_lots_disponibles()
    {
        var lots = new[]
        {
            L("1", 300, 15_000), L("2", 300, 18_000), L("3", 450, 15_000),
            L("4", 300, 25_000, s: StatutLot.Vendu), L("5", 300, 12_000, s: StatutLot.Reserve)
        };
        var r = ResumeLots.Calculer(lots);
        Assert.Equal((5, 3, 1, 1), (r.Total, r.Disponibles, r.Reserves, r.Vendus));
        Assert.Equal((15_000L, 18_000L), (r.PrixM2Min!.Value, r.PrixM2Max!.Value)); // lots vendu et réservé ignorés
        Assert.Equal((4_500_000L, 6_750_000L), (r.PrixMin!.Value, r.PrixMax!.Value));
        Assert.Equal((300m, 450m), (r.SurfaceMin!.Value, r.SurfaceMax!.Value));
        Assert.Equal(4_500_000, ResumeLots.PrixParent(lots));
    }

    [Fact]
    public void Fourchette_saisie_utilisee_tant_que_les_lots_ne_sont_pas_chiffres()
    {
        var r = ResumeLots.Calculer([L("1", 300, null), L("2", 300, null)], 12_000, 18_000);
        Assert.Equal((12_000L, 18_000L), (r.PrixM2Min!.Value, r.PrixM2Max!.Value));
        Assert.Null(ResumeLots.PrixParent([L("1", 300, null)]));
    }

    [Fact]
    public void Tout_vendu_le_prix_parent_reste_le_moins_cher()
    {
        Assert.Equal(4_500_000, ResumeLots.PrixParent([L("1", 300, 15_000, s: StatutLot.Vendu), L("2", 300, 20_000, s: StatutLot.Vendu)]));
    }

    [Fact]
    public void Numeros_tries_dans_l_ordre_naturel()
    {
        var tries = LotissementService.Trier([L("10", 1, 1), L("A-2", 1, 1), L("2", 1, 1), L("A-10", 1, 1), L("1", 1, 1)]).Select(l => l.Numero);
        Assert.Equal(["1", "2", "10", "A-2", "A-10"], tries);
    }
}

public class LienCarteEtCommoditesTests
{
    [Theory]
    [InlineData("https://www.google.com/maps/place/Popenguine/@14.5591342,-17.1140053,15z/data=!3m1", 14.5591342, -17.1140053)]
    [InlineData("https://www.google.com/maps/place/X/data=!4m6!3m5!1s0x0:0x0!8m2!3d14.7128!4d-17.1768", 14.7128, -17.1768)]
    [InlineData("https://maps.google.com/?q=14.5655,-17.1030", 14.5655, -17.1030)]
    [InlineData("https://consent.google.com/m?continue=https://www.google.com/maps/%4014.1262,-16.8535,17z&gl=SN", 14.1262, -16.8535)]
    public void Position_lue_dans_les_liens_google_maps(string url, double lat, double lng)
    {
        Assert.Equal((lat, lng), LienCarte.Extraire(url));
    }

    [Theory]
    [InlineData("https://maps.app.goo.gl/AbCdEf123", true)]
    [InlineData("https://goo.gl/maps/AbCdEf123", true)]
    [InlineData("http://maps.app.goo.gl/AbCdEf123", false)]   // HTTPS uniquement
    [InlineData("https://evil.example/maps.app.goo.gl", false)]
    [InlineData("https://goo.gl/autre-chose", false)]
    public void Seuls_les_liens_courts_google_sont_resolus(string url, bool attendu)
    {
        Assert.Equal(attendu, LienCarte.EstLienCourt(url));
    }

    [Fact]
    public void Redirections_suivies_uniquement_vers_google()
    {
        Assert.True(LienCarte.HoteAutorise(new Uri("https://www.google.com/maps/place/x")));
        Assert.False(LienCarte.HoteAutorise(new Uri("https://169.254.169.254/latest/meta-data")));
        Assert.False(LienCarte.HoteAutorise(new Uri("https://google.com.evil.example/")));
    }

    [Fact]
    public void Commodite_distance_et_duree()
    {
        Assert.Equal("1,5 km · 5 min en voiture", new CommoditeVm("École", 1.5m, 5, ModeTrajet.Voiture).Texte);
        Assert.Equal("300 m · 4 min à pied", new CommoditeVm("Marché", 0.3m, 4, ModeTrajet.APied).Texte);
        Assert.Equal("10 min", new CommoditeVm("Plage", null, 10).Texte);
        Assert.Equal("2 km", new CommoditeVm("Route", 2m).Texte);
    }
}
