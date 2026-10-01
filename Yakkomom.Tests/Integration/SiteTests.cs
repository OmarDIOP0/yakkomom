using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Services;

namespace Yakkomom.Tests.Integration;

/// <summary>
/// Documents fonciers : « Les documents privés ne doivent JAMAIS être accessibles par URL directe
/// sans être connecté en admin. »
/// </summary>
[Collection(CollectionSite.Nom)]
public class DocumentsPrivesTests(SiteDeTest site)
{
    [Fact]
    public async Task Document_prive_introuvable_pour_le_public()
    {
        var r = await site.Client().GetAsync($"/documents/{site.D.DocPrive.Id}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.DoesNotContain("document de test", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Document_public_d_un_terrain_publie_accessible()
    {
        var r = await site.Client().GetAsync($"/documents/{site.D.DocPublic.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/pdf", r.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Document_public_d_un_brouillon_reste_cache()
    {
        var r = await site.Client().GetAsync($"/documents/{site.D.DocPublicSurBrouillon.Id}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Fichier_prive_jamais_servi_par_son_chemin_de_stockage()
    {
        var c = site.Client();
        foreach (var chemin in new[] { "/media/" + site.D.DocPrive.CleStockage, "/prive/" + site.D.DocPrive.CleStockage,
                                       "/App_Data/prive/" + site.D.DocPrive.CleStockage, "/media/../prive/" + site.D.DocPrive.CleStockage })
            Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(chemin)).StatusCode);
    }

    [Fact]
    public async Task Lien_admin_d_un_document_exige_la_connexion()
    {
        var r = await site.Client().GetAsync($"/admin/documents/{site.D.DocPrive.Id}");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/admin/connexion", r.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Admin_connecte_ouvre_le_document_prive()
    {
        var admin = await site.ClientAdminAsync();
        var r = await admin.GetAsync($"/admin/documents/{site.D.DocPrive.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("document de test", await r.Content.ReadAsStringAsync());
        Assert.Contains("no-store", r.Headers.CacheControl?.ToString());
    }
}

/// <summary>Liste publique : filtres, et brouillons jamais visibles.</summary>
[Collection(CollectionSite.Nom)]
public class FiltresEtVisibiliteTests(SiteDeTest site)
{
    [Fact]
    public async Task Filtre_prix_maximum()
    {
        var html = await site.Client().GetStringAsync("/terrains?prixmax=15000000");
        Assert.Contains("YK-9001", html);
        Assert.DoesNotContain("YK-9002", html);
    }

    [Fact]
    public async Task Filtre_prix_minimum_saisi_avec_espaces()
    {
        var html = await site.Client().GetStringAsync("/terrains?prixmin=" + Uri.EscapeDataString("20 000 000"));
        Assert.Contains("YK-9002", html);
        Assert.DoesNotContain("YK-9001", html);
    }

    [Fact]
    public async Task Recherche_par_reference()
    {
        var html = await site.Client().GetStringAsync("/terrains?q=YK-9002");
        Assert.Contains("YK-9002", html);
        Assert.DoesNotContain("YK-9001", html);
    }

    [Fact]
    public async Task Brouillon_absent_de_la_liste_du_sitemap_et_de_sa_fiche()
    {
        var c = site.Client();
        Assert.DoesNotContain("YK-9003", await c.GetStringAsync("/terrains"));
        Assert.DoesNotContain("yk-9003", await c.GetStringAsync("/sitemap.xml"));
        var fiche = await c.GetAsync(UrlTerrain.Chemin("YK-9003", 500, null));
        Assert.Equal(HttpStatusCode.NotFound, fiche.StatusCode);
    }

    [Fact]
    public async Task Fiche_publiee_accessible_et_ancienne_url_redirigee_vers_la_canonique()
    {
        var c = site.Client();
        var chemin = UrlTerrain.Chemin("YK-9001", 500, null);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(chemin)).StatusCode);
        var ancienne = await c.GetAsync("/terrains/yk-9001-ancien-titre");
        Assert.Equal(HttpStatusCode.MovedPermanently, ancienne.StatusCode);
        Assert.EndsWith(chemin, ancienne.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Pages_filtrees_non_indexees()
    {
        var html = await site.Client().GetStringAsync("/terrains?prixmax=15000000");
        Assert.Contains("noindex", html);
    }
}

/// <summary>Liens WhatsApp : message pré-rempli et clics comptés sans données personnelles.</summary>
[Collection(CollectionSite.Nom)]
public class WhatsAppTests(SiteDeTest site)
{
    private async Task<int> ClicsAsync(int terrainId)
    {
        using var scope = site.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<YakkomomDbContext>().ClicsWhatsApp.CountAsync(c => c.TerrainId == terrainId);
    }

    [Fact]
    public async Task Redirection_vers_wame_avec_reference_et_lien_de_la_fiche()
    {
        var r = await site.Client().GetAsync("/wa/YK-9002/1?source=fiche");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var cible = Uri.UnescapeDataString(r.Headers.Location!.ToString());
        Assert.StartsWith("https://wa.me/221700000001?text=", cible);
        Assert.Contains("YK-9002", cible);
        Assert.Contains("https://yakkomom.test/terrains/yk-9002", cible);
        Assert.Contains("Bonjour Yakkomom", cible);
    }

    [Fact]
    public async Task Clic_compte_une_seule_fois_par_visiteur_et_jamais_pour_les_robots()
    {
        var avant = await ClicsAsync(site.D.Abordable.Id);
        var c = site.Client();
        await c.GetAsync("/wa/YK-9001/1?source=fiche");
        await c.GetAsync("/wa/YK-9001/1?source=fiche"); // double clic : ignoré
        var robot = site.Client(navigateur: false);
        robot.DefaultRequestHeaders.UserAgent.ParseAdd("Googlebot/2.1");
        await robot.GetAsync("/wa/YK-9001/1");
        Assert.Equal(avant + 1, await ClicsAsync(site.D.Abordable.Id));
    }

    [Fact]
    public async Task Aucun_champ_personnel_dans_la_table_des_clics()
    {
        using var scope = site.Services.CreateScope();
        var type = scope.ServiceProvider.GetRequiredService<YakkomomDbContext>().Model.FindEntityType(typeof(Models.Entities.ClicWhatsApp))!;
        var colonnes = type.GetProperties().Select(p => p.Name.ToLowerInvariant()).ToList();
        Assert.DoesNotContain(colonnes, c => c.Contains("ip") || c.Contains("agent") || c.Contains("visiteur") || c.Contains("telephone"));
    }

    [Fact]
    public async Task Terrain_inconnu_ou_brouillon_sans_redirection_whatsapp()
    {
        var c = site.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/wa/YK-9999/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/wa/YK-9003/1")).StatusCode);
    }
}

/// <summary>Espace admin, en-têtes de sécurité, jeton anti-falsification.</summary>
[Collection(CollectionSite.Nom)]
public class SecuriteTests(SiteDeTest site)
{
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/terrains")]
    [InlineData("/admin/journal")]
    [InlineData("/admin/utilisateurs")]
    [InlineData("/admin/parametres")]
    public async Task Admin_inaccessible_sans_connexion(string chemin)
    {
        var r = await site.Client().GetAsync(chemin);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("/admin/connexion", r.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Envoi_de_fichier_sans_connexion_refuse_en_json()
    {
        var c = site.Client();
        c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var r = await c.PostAsync($"/admin/terrains/{site.D.Abordable.Id}/photos/envoi", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Mauvais_mot_de_passe_message_generique()
    {
        var c = site.Client();
        var page = await c.GetStringAsync("/admin/connexion");
        var r = await c.PostAsync("/admin/connexion", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = site.EmailAdmin, ["MotDePasse"] = "mauvais-mot-de-passe-1", ["__RequestVerificationToken"] = SiteDeTest.Jeton(page)
        }));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode); // reste sur la page de connexion
        var html = await r.Content.ReadAsStringAsync();
        Assert.Contains("incorrect", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Formulaire_admin_sans_jeton_anti_falsification_rejete()
    {
        var admin = await site.ClientAdminAsync();
        var r = await admin.PostAsync("/admin/parametres", new FormUrlEncodedContent(new Dictionary<string, string> { ["NomSite"] = "Piratage" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Pages_admin_non_indexees_et_jamais_en_cache()
    {
        var admin = await site.ClientAdminAsync();
        var r = await admin.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("noindex", r.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Contains("no-store", r.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/terrains")]
    [InlineData("/admin/connexion")]
    [InlineData("/page-qui-n-existe-pas")]
    public async Task En_tetes_de_securite_sur_toutes_les_reponses(string chemin)
    {
        var r = await site.Client().GetAsync(chemin);
        var csp = string.Join("", r.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Matches("script-src 'self' 'nonce-[0-9A-F]{32}'", csp);
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", r.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(r.Headers.Contains("Strict-Transport-Security"), "HSTS attendu en production");
        Assert.False(r.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Nonce_de_l_import_map_identique_a_celui_de_la_politique()
    {
        var r = await site.Client().GetAsync("/");
        var csp = string.Join("", r.Headers.GetValues("Content-Security-Policy"));
        var nonce = System.Text.RegularExpressions.Regex.Match(csp, "'nonce-([^']+)'").Groups[1].Value;
        Assert.Contains($"<script type=\"importmap\" nonce=\"{nonce}\">", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Donnees_structurees_protegees_contre_un_titre_piege()
    {
        using var scope = site.Services.CreateScope();
        var seo = scope.ServiceProvider.GetRequiredService<Seo>();
        var json = seo.FilAriane(("</script><script>alert(1)</script>", "/x"));
        Assert.DoesNotContain("</script>", json);
    }

    [Fact]
    public async Task Http_redirige_vers_https()
    {
        var c = site.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
        var r = await c.GetAsync("/terrains");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, r.StatusCode);
        Assert.StartsWith("https://", r.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health")).StatusCode); // sonde de l'hébergeur en HTTP
    }
}

/// <summary>Grand terrain découpé en lots.</summary>
[Collection(CollectionSite.Nom)]
public class LotissementTests(SiteDeTest site)
{
    [Fact]
    public async Task Fiche_affiche_les_lots_la_fourchette_et_un_bouton_whatsapp_par_lot_disponible()
    {
        var html = await site.Client().GetStringAsync(UrlTerrain.Chemin("YK-9004", 10_000, null));
        Assert.Contains("À partir de", html);
        Assert.Contains("2 lots disponibles", html);
        Assert.Contains("Lot 2", html);
        Assert.Contains("lot=2", html);
        Assert.DoesNotContain("lot=3", html); // lot vendu : pas de bouton WhatsApp
    }

    [Fact]
    public async Task Message_whatsapp_precise_le_lot()
    {
        var r = await site.Client().GetAsync("/wa/YK-9004/1?source=fiche&lot=2");
        Assert.Contains("(lot 2, 300", Uri.UnescapeDataString(r.Headers.Location!.ToString()));
    }

    [Fact]
    public async Task Carte_de_la_liste_annonce_les_lots_disponibles()
    {
        var html = await site.Client().GetStringAsync("/terrains?q=YK-9004");
        Assert.Contains("2 lots disponibles", html);
        var e = Format.Insecable;
        Assert.Contains($"15{e}000 à 18{e}000{e}FCFA/m²", WebUtility.HtmlDecode(html)); // fourchette au m² des lots disponibles
    }

    [Fact]
    public async Task Admin_cree_une_serie_de_lots_et_le_prix_a_partir_de_suit()
    {
        // Terrain dédié : ne pas modifier ceux des autres tests (filtres de prix)
        int id;
        using (var s = site.Services.CreateScope())
        {
            var db0 = s.ServiceProvider.GetRequiredService<YakkomomDbContext>();
            var t = new Models.Entities.Terrain
            {
                Reference = "YK-9010", Titre = "Grand terrain à découper", Statut = Models.Enums.StatutTerrain.Disponible,
                Type = Models.Enums.TypeTerrain.Lotissement, Prix = 99_000_000, PublieLe = DateTime.UtcNow
            };
            db0.Terrains.Add(t);
            await db0.SaveChangesAsync();
            id = t.Id;
        }
        var admin = await site.ClientAdminAsync();

        var page = await admin.GetStringAsync($"/admin/terrains/{id}/lots");
        var r = await admin.PostAsync($"/admin/terrains/{id}/lots/serie", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Serie.Debut"] = "1", ["Serie.Fin"] = "5", ["Serie.SurfaceM2"] = "400", ["Serie.PrixM2"] = "10 000",
            ["__RequestVerificationToken"] = SiteDeTest.Jeton(page)
        }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

        using var scope = site.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YakkomomDbContext>();
        Assert.Equal(5, await db.Lots.CountAsync(l => l.TerrainId == id));
        Assert.Equal(4_000_000, (await db.Terrains.AsNoTracking().SingleAsync(t => t.Id == id)).Prix);
    }
}
