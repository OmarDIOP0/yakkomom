using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>
/// Document foncier (PDF ou image). Privé par défaut : le public ne voit qu'un badge
/// et ne peut JAMAIS télécharger un document privé.
/// </summary>
public class DocumentFoncier
{
    public int Id { get; set; }
    public int TerrainId { get; set; }
    public Terrain Terrain { get; set; } = null!;

    public TypeDocumentFoncier Type { get; set; }
    public string? Titre { get; set; }
    public string? Notes { get; set; }

    public string CleStockage { get; set; } = string.Empty;
    public string NomFichierOriginal { get; set; } = string.Empty;
    public string TypeMime { get; set; } = string.Empty;
    public long TailleOctets { get; set; }

    public bool EstPublic { get; set; }
    /// <summary>Identifiant d'envoi généré par le navigateur (anti-doublon en cas de reprise).</summary>
    public Guid? IdEnvoi { get; set; }

    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public string? CreeParId { get; set; }
}
