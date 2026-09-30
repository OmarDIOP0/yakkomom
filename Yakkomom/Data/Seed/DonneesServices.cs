namespace Yakkomom.Data.Seed;

/// <summary>Les 7 services Yakkomom (textes de départ, modifiables depuis l'admin).</summary>
internal static class DonneesServices
{
    public record ServiceInitial(string Titre, string Icone, string Resume, string Description);

    public static readonly ServiceInitial[] Services =
    [
        new("Vente de terrains", "terrain",
            "Des terrains vérifiés, bornés et documentés, partout au Sénégal.",
            "Nous sélectionnons chaque terrain avant de le proposer : vérification des documents fonciers, visite sur place, relevé GPS et photos récentes.\n\n" +
            "Vous visitez à distance grâce aux photos, à la visite 360° et à la carte satellite, puis nous organisons une visite physique quand vous êtes prêt."),
        new("Construction de maisons", "maison",
            "De la fondation aux finitions, votre maison construite selon votre budget.",
            "Nous vous accompagnons de la conception des plans jusqu'à la remise des clés : étude du sol, plans, devis détaillé, suivi de chantier et reportage photo régulier.\n\n" +
            "Idéal pour la diaspora : vous suivez l'avancement de votre maison depuis l'étranger, étape par étape."),
        new("Bâtiments et ouvrages", "batiment",
            "Immeubles, locaux commerciaux, clôtures et ouvrages sur mesure.",
            "Immeubles de rapport, commerces, entrepôts, murs de clôture, forages : nous réalisons vos ouvrages avec des équipes qualifiées et des matériaux contrôlés."),
        new("Accompagnement personnalisé", "accompagnement",
            "Un conseiller dédié du premier appel jusqu'à la signature.",
            "Un conseiller vous suit du début à la fin : définition de votre besoin, recherche du terrain adapté, visites, négociation et démarches administratives.\n\n" +
            "Vous avez un interlocuteur unique, joignable sur WhatsApp."),
        new("Sécurisation des transactions", "bouclier",
            "Vérification foncière, notaire et bornage : achetez sans risque.",
            "Nous vérifions l'authenticité des documents (titre foncier, bail, délibération), la situation au cadastre et l'absence de litige avant toute transaction.\n\n" +
            "Le paiement et la signature se font chez le notaire. Vous ne versez jamais d'argent sans document vérifié."),
        new("Investissement durable", "croissance",
            "Investissez dans les zones à fort potentiel de valorisation.",
            "Nous vous conseillons sur les zones en développement (pôle urbain de Diamniadio, Petite-Côte, axes des nouvelles autoroutes) pour un investissement qui prend de la valeur dans le temps."),
        new("Champs et aménagement agricole", "champ",
            "Terres agricoles, forages, clôtures et mise en valeur de vos champs.",
            "Achat de terres agricoles, étude des sols, forage, irrigation, clôture et mise en culture : nous vous aidons à rendre votre champ productif."),
    ];
}
