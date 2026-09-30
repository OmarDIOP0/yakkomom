using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>
/// Clic sur un lien WhatsApp, pour les statistiques. Aucune donnée personnelle
/// (ni IP, ni user-agent, ni identifiant du visiteur).
/// </summary>
public class ClicWhatsApp
{
    public long Id { get; set; }
    public int? TerrainId { get; set; }
    public Terrain? Terrain { get; set; }
    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    /// <summary>Numéro choisi (1, 2 ou 3).</summary>
    public short NumeroIndex { get; set; }
    public SourceClicWhatsApp Source { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}
