using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

public class TerrainPublicService(
    YakkomomDbContext db,
    IParametreSiteService parametres,
    ImagesPubliques images,
    IVisite360Service visite360,
    Seo seo,
    UrlSite urls) : ITerrainPublicService
{
    public const int TaillePage = 12;
    // Conditions explicites plutôt que StatutsPublics.Contains(t.Statut) : combiné à un second filtre
    // sur le statut, EF Core 10 (énumérations stockées en texte) simplifiait la requête en « WHERE FALSE ».
    private IQueryable<Terrain> Publics => db.Terrains.AsNoTracking()
        .Where(t => t.Statut == StatutTerrain.Disponible || t.Statut == StatutTerrain.Reserve || t.Statut == StatutTerrain.Vendu);

    // =====================================================================
    // Liste
    // =====================================================================
    public async Task<ListeTerrainsPublicVm> ListerAsync(FiltreTerrainsPublic f, CancellationToken ct = default)
    {
        var q = Publics;

        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var motif = "%" + f.Q.Trim().Replace("%", "").Replace("_", "") + "%";
            q = q.Where(t => EF.Functions.ILike(t.Reference, motif) || EF.Functions.ILike(t.Titre, motif)
                || (t.QuartierVillage != null && EF.Functions.ILike(t.QuartierVillage, motif))
                || (t.Commune != null && EF.Functions.ILike(t.Commune.Nom, motif))
                || (t.Departement != null && EF.Functions.ILike(t.Departement.Nom, motif))
                || (t.Region != null && EF.Functions.ILike(t.Region.Nom, motif)));
        }
        if (f.Region is { } region) q = q.Where(t => t.RegionId == region);
        if (f.Commune is { } commune) q = q.Where(t => t.CommuneId == commune);
        if (f.Type is { } type) q = q.Where(t => t.Type == type);
        if (f.Statut == "disponible") q = q.Where(t => t.Statut == StatutTerrain.Disponible);
        if (Nombres.LireMontant(f.PrixMin, out var prixMin) && prixMin is not null) q = q.Where(t => t.Prix >= prixMin);
        if (Nombres.LireMontant(f.PrixMax, out var prixMax) && prixMax is not null) q = q.Where(t => t.Prix <= prixMax);
        if (Nombres.LireDecimal(f.SurfaceMin, out var sMin) && sMin is not null) q = q.Where(t => t.SurfaceM2 >= sMin);
        if (Nombres.LireDecimal(f.SurfaceMax, out var sMax) && sMax is not null) q = q.Where(t => t.SurfaceM2 <= sMax);
        if (f.Visite360) q = q.Where(t => t.Panoramas.Any());
        if (f.TitreFoncier)
            q = q.Where(t => t.SituationFonciere == TypeDocumentFoncier.TitreFoncier || t.Documents.Any(d => d.Type == TypeDocumentFoncier.TitreFoncier));

        // Par défaut : disponibles d'abord (les vendus restent visibles en fin de liste : preuve sociale)
        q = f.Tri switch
        {
            "prix" => q.OrderBy(t => t.Prix == null).ThenBy(t => t.Prix),
            "prix-desc" => q.OrderBy(t => t.Prix == null).ThenByDescending(t => t.Prix),
            "surface" => q.OrderBy(t => t.SurfaceM2 == null).ThenBy(t => t.SurfaceM2),
            "surface-desc" => q.OrderBy(t => t.SurfaceM2 == null).ThenByDescending(t => t.SurfaceM2),
            _ => q.OrderBy(t => t.Statut == StatutTerrain.Disponible ? 0 : t.Statut == StatutTerrain.Reserve ? 1 : 2)
                  .ThenByDescending(t => t.PublieLe).ThenByDescending(t => t.Id)
        };

        var total = await q.CountAsync(ct);
        var nombrePages = Math.Max(1, (int)Math.Ceiling(total / (double)TaillePage));
        var page = Math.Clamp(f.Page, 1, nombrePages);
        var cartes = await ProjeterCartes(q.Skip((page - 1) * TaillePage).Take(TaillePage), ct);

        // Lieux proposés dans les filtres : seulement ceux qui ont des terrains publiés
        var lieux = await Publics
            .GroupBy(t => new { t.RegionId, RegionNom = t.Region!.Nom, t.CommuneId, CommuneNom = t.Commune!.Nom, t.DepartementId })
            .Select(g => new { g.Key.RegionId, g.Key.RegionNom, g.Key.CommuneId, g.Key.CommuneNom, g.Key.DepartementId, N = g.Count() })
            .ToListAsync(ct);
        var departements = await db.Localites.AsNoTracking()
            .Where(l => l.Type == TypeLocalite.Departement).ToDictionaryAsync(l => l.Id, l => l.Nom, ct);

        return new ListeTerrainsPublicVm
        {
            Filtre = f,
            Terrains = cartes.Select((c, i) => i < 2 ? Prioritaire(c) : c).ToList(),
            Total = total,
            Page = page,
            NombrePages = nombrePages,
            Regions = lieux.Where(l => l.RegionId != null).GroupBy(l => (l.RegionId!.Value, l.RegionNom))
                .Select(g => new OptionLieu(g.Key.Value, g.Key.RegionNom, null, g.Sum(x => x.N))).OrderBy(o => o.Nom).ToList(),
            Communes = lieux.Where(l => l.CommuneId != null && (f.Region == null || l.RegionId == f.Region)).GroupBy(l => (l.CommuneId!.Value, l.CommuneNom, l.DepartementId))
                .Select(g => new OptionLieu(g.Key.Value, g.Key.CommuneNom, g.Key.DepartementId, g.Sum(x => x.N))).OrderBy(o => o.Nom).ToList(),
            NomsDepartements = departements
        };
    }

    // =====================================================================
    // Fiche
    // =====================================================================
    public async Task<FicheTerrainVm?> FicheAsync(string reference, bool apercuAdmin, CancellationToken ct = default)
    {
        var t = await db.Terrains.AsNoTracking().AsSplitQuery()
            .Include(x => x.Photos.OrderByDescending(p => p.EstCouverture).ThenBy(p => p.Ordre))
            .Include(x => x.Documents.Where(d => d.EstPublic))
            .Include(x => x.Region).Include(x => x.Departement).Include(x => x.Commune)
            .FirstOrDefaultAsync(x => x.Reference == reference, ct);
        if (t is null) return null;
        var estPublic = t.EstPublic;
        if (!estPublic && !apercuAdmin) return null;

        var p = await parametres.ObtenirAsync(ct);
        var lieuCourt = t.QuartierVillage ?? t.Commune?.Nom ?? t.Departement?.Nom ?? t.Region?.Nom;
        var resume = WhatsAppLiens.Resume(t.SurfaceM2, t.Commune?.Nom ?? lieuCourt);
        var chemin = UrlTerrain.Chemin(t.Reference, t.SurfaceM2, t.Commune?.Nom);
        var nbPanoramas = await db.Panoramas.CountAsync(x => x.TerrainId == t.Id, ct);
        var numeros = WhatsAppLiens.NumerosEffectifs(t.Contacts, p.ContactsParDefaut);
        var lots = LotissementService.Trier(await db.Lots.AsNoTracking().Where(l => l.TerrainId == t.Id).ToListAsync(ct)).ToList();
        var plan = t.Documents.Where(d => d.Type == TypeDocumentFoncier.PlanLotissement).OrderByDescending(d => d.TypeMime.StartsWith("image/")).FirstOrDefault();

        return new FicheTerrainVm
        {
            Reference = t.Reference, Titre = t.Titre, Description = t.Description, Type = t.Type, Statut = t.Statut,
            Prix = t.Prix, PrixNegociable = t.PrixNegociable, SurfaceM2 = t.SurfaceM2, SurfaceCalculeeM2 = t.SurfaceCalculeeM2,
            LongueurM = t.LongueurM, LargeurM = t.LargeurM, SituationFonciere = t.SituationFonciere,
            Region = t.Region?.Nom, Departement = t.Departement?.Nom, Commune = t.Commune?.Nom,
            QuartierVillage = t.QuartierVillage, Adresse = t.Adresse,
            Latitude = t.Latitude, Longitude = t.Longitude, ContourGeoJson = t.ContourGeoJson,
            AccesEau = t.AccesEau, AccesElectricite = t.AccesElectricite, RouteAcces = t.RouteAcces, RouteAccesDetail = t.RouteAccesDetail,
            Commodites = t.Commodites.OrderBy(c => c.DistanceKm ?? decimal.MaxValue).ThenBy(c => c.DureeMinutes ?? int.MaxValue)
                .Select(c => new CommoditeVm(c.Libelle, c.DistanceKm, c.DureeMinutes, c.Mode)).ToList(),
            Photos = t.Photos.Select(images.Photo).ToList(),
            NombrePanoramas = nbPanoramas,
            Visite = nbPanoramas > 0 ? await visite360.VisitePubliqueAsync(t.Id, ct) : null,
            VideoId = YouTube.ExtraireId(t.VideoYoutubeUrl),
            DocumentsPublics = t.Documents.OrderBy(d => d.Type)
                .Select(d => new DocumentPublicVm(d.Id, NomType(d.Type), d.Titre, $"/documents/{d.Id}")).ToList(),
            AUnTitreFoncier = t.SituationFonciere == TypeDocumentFoncier.TitreFoncier
                || await db.DocumentsFonciers.AnyAsync(d => d.TerrainId == t.Id && d.Type == TypeDocumentFoncier.TitreFoncier, ct),
            Lots = lots.Select(l => new LotPublicVm(l.Numero, l.SurfaceM2, l.PrixM2Effectif, l.PrixEffectif, l.Position, l.Statut,
                numeros.Count > 0 && l.Statut != StatutLot.Vendu ? $"/wa/{t.Reference}/{numeros[0].Index}?source=fiche&lot={Uri.EscapeDataString(l.Numero)}" : null)).ToList(),
            ResumeLots = ResumeLots.Calculer(lots, t.PrixM2Min, t.PrixM2Max),
            PlanLotissement = plan is null ? null : new DocumentPublicVm(plan.Id, NomType(plan.Type), plan.Titre, $"/documents/{plan.Id}"),
            PlanEstImage = plan?.TypeMime.StartsWith("image/") == true,
            WhatsApp = numeros.Select(n => new ContactWhatsAppVm(n.Index, TelephoneSenegal.Afficher(n.Numero), n.Libelle,
                $"/wa/{t.Reference}/{n.Index}")).ToList(),
            UrlDemandeDocument = numeros.Count > 0 ? $"/wa/{t.Reference}/{numeros[0].Index}?motif=document" : null,
            Email = WhatsAppLiens.EmailEffectif(t.Contacts, p.ContactsParDefaut),
            UrlCanonique = urls.Absolue(chemin),
            Resume = resume,
            Similaires = estPublic ? await SimilairesAsync(t, ct) : [],
            ModifieLe = t.ModifieLe,
            PublieLe = t.PublieLe,
            ImagePartage = t.Photos.FirstOrDefault() is { } couverture
                ? seo.ImagePhoto(couverture, $"{t.Titre} — {resume}")
                : seo.ImageParDefaut(p.NomSite),
            EstApercuAdmin = !estPublic
        };
    }

    private async Task<IReadOnlyList<CarteTerrainVm>> SimilairesAsync(Terrain t, CancellationToken ct)
    {
        var q = Publics.Where(x => x.Id != t.Id && x.Statut != StatutTerrain.Vendu);
        IQueryable<Terrain>? proches =
            t.CommuneId is not null ? q.Where(x => x.CommuneId == t.CommuneId) :
            t.DepartementId is not null ? q.Where(x => x.DepartementId == t.DepartementId) :
            t.RegionId is not null ? q.Where(x => x.RegionId == t.RegionId) : null;

        var resultat = proches is null ? [] : await ProjeterCartes(proches.OrderBy(x => x.Statut).ThenByDescending(x => x.PublieLe).Take(3), ct);
        if (resultat.Count < 3 && t.RegionId is not null)
        {
            var deja = resultat.Select(c => c.Reference).ToList();
            var complement = await ProjeterCartes(q.Where(x => x.RegionId == t.RegionId && !deja.Contains(x.Reference))
                .OrderByDescending(x => x.PublieLe).Take(3 - resultat.Count), ct);
            resultat = [.. resultat, .. complement];
        }
        return resultat;
    }

    // =====================================================================
    // Accueil et favoris
    // =====================================================================
    public async Task<AccueilVm> AccueilAsync(CancellationToken ct = default)
    {
        var enAvant = await ProjeterCartes(Publics.Where(t => t.EstMisEnAvant && t.Statut != StatutTerrain.Vendu)
            .OrderBy(t => t.OrdreMiseEnAvant).ThenByDescending(t => t.PublieLe).Take(6), ct);
        if (enAvant.Count < 6)
        {
            var deja = enAvant.Select(c => c.Reference).ToList();
            var recents = await ProjeterCartes(Publics.Where(t => t.Statut == StatutTerrain.Disponible && !deja.Contains(t.Reference))
                .OrderByDescending(t => t.PublieLe).Take(6 - enAvant.Count), ct);
            enAvant = [.. enAvant, .. recents];
        }

        var services = await db.Services.AsNoTracking().Where(s => s.EstActif).OrderBy(s => s.Ordre)
            .Select(s => new ServiceResumeVm(s.Titre, s.Slug, s.Resume, s.Icone)).ToListAsync(ct);
        var comptes = await Publics.GroupBy(t => t.Statut).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
        var p = await parametres.ObtenirAsync(ct);
        var principal = WhatsAppLiens.NumerosEffectifs(null, p.ContactsParDefaut).FirstOrDefault();

        return new AccueilVm
        {
            ALaUne = enAvant.Select((c, i) => i < 2 ? Prioritaire(c) : c).ToList(),
            Services = services,
            NombreDisponibles = comptes.FirstOrDefault(c => c.Key == StatutTerrain.Disponible)?.N ?? 0,
            NombreVendus = comptes.FirstOrDefault(c => c.Key == StatutTerrain.Vendu)?.N ?? 0,
            WhatsAppPrincipal = principal is null ? null
                : new ContactWhatsAppVm(principal.Index, TelephoneSenegal.Afficher(principal.Numero), principal.Libelle, $"/wa/general/{principal.Index}")
        };
    }

    public async Task<IReadOnlyList<CarteTerrainVm>> CartesAsync(IReadOnlyList<string> references, CancellationToken ct = default)
    {
        var refs = references.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim().ToUpperInvariant()).Distinct().Take(100).ToList();
        if (refs.Count == 0) return [];
        var cartes = await ProjeterCartes(Publics.Where(t => refs.Contains(t.Reference)), ct);
        return cartes.OrderBy(c => refs.IndexOf(c.Reference)).ToList();
    }

    // =====================================================================
    // Projection commune des cartes
    // =====================================================================
    private async Task<List<CarteTerrainVm>> ProjeterCartes(IQueryable<Terrain> requete, CancellationToken ct)
    {
        var lignes = await requete.Select(t => new
        {
            t.Reference, t.Titre, t.Statut, t.Prix, t.SurfaceM2, t.QuartierVillage,
            Commune = t.Commune != null ? t.Commune.Nom : null,
            Departement = t.Departement != null ? t.Departement.Nom : null,
            Couverture = t.Photos.OrderByDescending(p => p.EstCouverture).ThenBy(p => p.Ordre).FirstOrDefault(),
            TitreFoncier = t.SituationFonciere == TypeDocumentFoncier.TitreFoncier || t.Documents.Any(d => d.Type == TypeDocumentFoncier.TitreFoncier),
            Lots = t.Lots.Select(l => new { l.Statut, l.Prix, l.PrixM2, l.SurfaceM2 }).ToList(),
            t.PrixM2Min, t.PrixM2Max,
            Visite360 = t.Panoramas.Any()
        }).ToListAsync(ct);

        return lignes.Select(l =>
        {
            var photo = l.Couverture is null ? null : images.Photo(l.Couverture);
            var lieu = string.Join(", ", new[] { l.QuartierVillage, l.Commune ?? l.Departement }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
            return new CarteTerrainVm
            {
                Reference = l.Reference,
                Titre = l.Titre,
                Url = UrlTerrain.Chemin(l.Reference, l.SurfaceM2, l.Commune),
                Localisation = lieu.Length > 0 ? lieu : null,
                Prix = l.Prix,
                SurfaceM2 = l.SurfaceM2,
                Statut = l.Statut,
                ImageUrl = photo?.Src,
                ImageSrcset = photo?.Srcset,
                ImageLargeur = photo?.Largeur ?? 800,
                ImageHauteur = photo?.Hauteur ?? 600,
                CouleurDominante = photo?.Couleur,
                TitreFoncier = l.TitreFoncier,
                Visite360 = l.Visite360,
                Lots = ResumeLots.Calculer(l.Lots.Select(x => new Lot { Statut = x.Statut, Prix = x.Prix, PrixM2 = x.PrixM2, SurfaceM2 = x.SurfaceM2 }).ToList(),
                    l.PrixM2Min, l.PrixM2Max)
            };
        }).ToList();
    }

    private static CarteTerrainVm Prioritaire(CarteTerrainVm c) => new()
    {
        Reference = c.Reference, Titre = c.Titre, Url = c.Url, Localisation = c.Localisation, Prix = c.Prix, SurfaceM2 = c.SurfaceM2,
        Statut = c.Statut, ImageUrl = c.ImageUrl, ImageSrcset = c.ImageSrcset, ImageLargeur = c.ImageLargeur, ImageHauteur = c.ImageHauteur,
        CouleurDominante = c.CouleurDominante, TitreFoncier = c.TitreFoncier, Visite360 = c.Visite360, Lots = c.Lots, Prioritaire = true
    };

    public static string NomType(TypeDocumentFoncier type) => type switch
    {
        TypeDocumentFoncier.TitreFoncier => "Titre foncier",
        TypeDocumentFoncier.Bail => "Bail",
        TypeDocumentFoncier.Deliberation => "Délibération",
        TypeDocumentFoncier.ActeCession => "Acte de cession",
        TypeDocumentFoncier.PlanBornage => "Plan de bornage",
        TypeDocumentFoncier.PlanLotissement => "Plan de lotissement",
        TypeDocumentFoncier.ExtraitCadastral => "Extrait cadastral / NICAD",
        TypeDocumentFoncier.CertificatInscription => "Certificat d'inscription",
        TypeDocumentFoncier.AutorisationConstruire => "Autorisation de construire",
        _ => "Document"
    };
}
