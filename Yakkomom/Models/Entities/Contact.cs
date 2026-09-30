namespace Yakkomom.Models.Entities;

/// <summary>
/// Jeu de contacts (3 WhatsApp + e-mail). Type possédé : utilisé à la fois par
/// <see cref="Terrain"/> et par <see cref="ParametreSite"/> (contacts par défaut).
/// Les numéros sont stockés normalisés au format international (+221…).
/// </summary>
public class Contact
{
    public string? WhatsApp1 { get; set; }
    public string? WhatsApp1Libelle { get; set; }
    public string? WhatsApp2 { get; set; }
    public string? WhatsApp2Libelle { get; set; }
    public string? WhatsApp3 { get; set; }
    public string? WhatsApp3Libelle { get; set; }
    public string? Email { get; set; }

    public bool EstVide =>
        string.IsNullOrWhiteSpace(WhatsApp1) && string.IsNullOrWhiteSpace(WhatsApp2) &&
        string.IsNullOrWhiteSpace(WhatsApp3) && string.IsNullOrWhiteSpace(Email);
}
