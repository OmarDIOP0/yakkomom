namespace Yakkomom.Models.Entities;

/// <summary>Paramètres globaux du site (une seule ligne, Id = 1).</summary>
public class ParametreSite
{
    public const int IdUnique = 1;

    public int Id { get; set; } = IdUnique;
    public string NomSite { get; set; } = "Yakkomom";
    public string? Slogan { get; set; }
    public string? CleLogo { get; set; }

    /// <summary>Contacts utilisés quand un terrain n'a pas les siens.</summary>
    public Contact ContactsParDefaut { get; set; } = new();

    /// <summary>
    /// Modèle du message WhatsApp pré-rempli. Jetons : {site}, {reference}, {resume}, {lien}.
    /// </summary>
    public string MessageWhatsAppTerrain { get; set; } =
        "Bonjour {site}, je suis intéressé(e) par le terrain {reference} ({resume}) : {lien}. Est-il toujours disponible ?";

    public string? Adresse { get; set; }
    public string? HorairesOuverture { get; set; }

    // --- Informations légales (page Mentions légales) ---------------------
    /// <summary>Nom officiel de l'entreprise (ex. « Yakkomom SARL »).</summary>
    public string? RaisonSociale { get; set; }
    /// <summary>SARL, SAS, SUARL, entreprise individuelle, GIE…</summary>
    public string? FormeJuridique { get; set; }
    public string? Ninea { get; set; }
    public string? Rccm { get; set; }
    public string? ResponsablePublication { get; set; }

    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? TikTok { get; set; }
    public string? YouTube { get; set; }
    public string? LinkedIn { get; set; }

    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;
}
