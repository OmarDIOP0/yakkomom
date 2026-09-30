using System.Net.Mail;
using Yakkomom.Helpers;
using Yakkomom.Models.Entities;

namespace Yakkomom.Services;

/// <summary>Validation et normalisation d'un jeu de contacts (terrain ou paramètres par défaut).</summary>
public static class ContactsSaisie
{
    public static ResultatOperation Appliquer(
        Contact cible,
        (string? Numero, string? Libelle) wa1,
        (string? Numero, string? Libelle) wa2,
        (string? Numero, string? Libelle) wa3,
        string? email,
        string prefixeChamp = "")
    {
        var resultat = new ResultatOperation();
        string? Numero(string champ, string? saisie)
        {
            if (TelephoneSenegal.Normaliser(saisie, out var n, out var erreur)) return n;
            resultat.AjouterErreur(prefixeChamp + champ, erreur!);
            return null;
        }

        var n1 = Numero("WhatsApp1", wa1.Numero);
        var n2 = Numero("WhatsApp2", wa2.Numero);
        var n3 = Numero("WhatsApp3", wa3.Numero);

        var courriel = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (courriel is not null && !MailAddress.TryCreate(courriel, out _))
            resultat.AjouterErreur(prefixeChamp + "Email", "Adresse e-mail invalide.");

        if (!resultat.Reussi) return resultat;

        cible.WhatsApp1 = n1; cible.WhatsApp1Libelle = Nettoyer(wa1.Libelle);
        cible.WhatsApp2 = n2; cible.WhatsApp2Libelle = Nettoyer(wa2.Libelle);
        cible.WhatsApp3 = n3; cible.WhatsApp3Libelle = Nettoyer(wa3.Libelle);
        cible.Email = courriel;
        return resultat;
    }

    private static string? Nettoyer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
