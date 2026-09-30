using System.ComponentModel.DataAnnotations;

namespace Yakkomom.Models.Enums;

public enum StatutTerrain
{
    [Display(Name = "Brouillon")] Brouillon = 0,
    [Display(Name = "Disponible")] Disponible = 1,
    [Display(Name = "Réservé")] Reserve = 2,
    [Display(Name = "Vendu")] Vendu = 3,
    [Display(Name = "Archivé")] Archive = 4
}

public enum TypeTerrain
{
    [Display(Name = "Habitation")] Habitation = 0,
    [Display(Name = "Commercial")] Commercial = 1,
    [Display(Name = "Agricole")] Agricole = 2,
    [Display(Name = "Lotissement")] Lotissement = 3
}

public enum TypeDocumentFoncier
{
    [Display(Name = "Titre foncier")] TitreFoncier = 0,
    [Display(Name = "Bail")] Bail = 1,
    [Display(Name = "Délibération")] Deliberation = 2,
    [Display(Name = "Acte de cession")] ActeCession = 3,
    [Display(Name = "Plan de bornage")] PlanBornage = 4,
    [Display(Name = "Plan de lotissement")] PlanLotissement = 5,
    [Display(Name = "Extrait cadastral / NICAD")] ExtraitCadastral = 6,
    [Display(Name = "Certificat d'inscription")] CertificatInscription = 7,
    [Display(Name = "Autorisation de construire")] AutorisationConstruire = 8,
    [Display(Name = "Autre")] Autre = 99
}

public enum TypeLocalite
{
    [Display(Name = "Région")] Region = 0,
    [Display(Name = "Département")] Departement = 1,
    [Display(Name = "Commune")] Commune = 2
}

public enum TypeHotspot
{
    /// <summary>Point cliquable qui mène à un autre panorama.</summary>
    [Display(Name = "Déplacement")] Navigation = 0,
    /// <summary>Simple bulle d'information.</summary>
    [Display(Name = "Information")] Information = 1
}

public enum SourceClicWhatsApp
{
    [Display(Name = "Fiche terrain")] FicheTerrain = 0,
    [Display(Name = "Bouton flottant")] BoutonFlottant = 1,
    [Display(Name = "Liste des terrains")] Liste = 2,
    [Display(Name = "Page service")] Service = 3,
    [Display(Name = "Général")] General = 4
}

public enum TypeAction
{
    [Display(Name = "Création")] Creation = 0,
    [Display(Name = "Modification")] Modification = 1,
    [Display(Name = "Suppression")] Suppression = 2,
    [Display(Name = "Changement de statut")] ChangementStatut = 3,
    [Display(Name = "Connexion")] Connexion = 4,
    [Display(Name = "Autre")] Autre = 99
}
