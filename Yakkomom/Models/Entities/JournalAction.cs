using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>Journal des actions admin : qui a modifié quoi et quand.</summary>
public class JournalAction
{
    public long Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string? UtilisateurId { get; set; }
    /// <summary>Copie du nom/e-mail, conservée même si le compte est supprimé.</summary>
    public string? UtilisateurNom { get; set; }

    public TypeAction Action { get; set; }
    /// <summary>Ex. « Terrain », « Service », « ParametreSite ».</summary>
    public string EntiteType { get; set; } = string.Empty;
    public string? EntiteId { get; set; }
    public string Description { get; set; } = string.Empty;
    /// <summary>Détails facultatifs (ex. champs modifiés), en jsonb.</summary>
    public string? DetailsJson { get; set; }
}
