using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;

namespace Yakkomom.Models.ViewModels.Admin;

// ---------------------------------------------------------------------------
// Tableau de bord
// ---------------------------------------------------------------------------
public class TableauDeBordVm
{
    /// <summary>Périodes proposées (en jours).</summary>
    public static readonly int[] Periodes = [7, 30, 90];

    public int Jours { get; init; }
    public required IReadOnlyDictionary<StatutTerrain, int> TerrainsParStatut { get; init; }
    public int TerrainsEnLigne => Compte(StatutTerrain.Disponible) + Compte(StatutTerrain.Reserve) + Compte(StatutTerrain.Vendu);
    public int Compte(StatutTerrain s) => TerrainsParStatut.TryGetValue(s, out var n) ? n : 0;

    public required StatistiquesClicsVm Clics { get; init; }

    public required IReadOnlyList<TerrainLigneVm> ACompleter { get; init; }
    public int TotalACompleter { get; init; }

    public required IReadOnlyList<JournalLigneVm> DernieresActions { get; init; }
}

public class StatistiquesClicsVm
{
    public int Total { get; init; }
    /// <summary>Total de la période précédente de même durée, pour la tendance.</summary>
    public int TotalPrecedent { get; init; }
    /// <summary>Un point par jour (heure de Dakar), jours sans clic compris.</summary>
    public required IReadOnlyList<(DateOnly Jour, int Clics)> ParJour { get; init; }
    public required IReadOnlyList<(SourceClicWhatsApp Source, int Clics)> ParSource { get; init; }
    public required IReadOnlyList<ClicsTerrainVm> ParTerrain { get; init; }
    public required IReadOnlyList<(int Id, string Titre, int Clics)> ParService { get; init; }
    /// <summary>Clics sans terrain ni service (bouton général, demande, contact).</summary>
    public int Generaux { get; init; }

    public int? Evolution => TotalPrecedent == 0 ? null : (int)Math.Round(100.0 * (Total - TotalPrecedent) / TotalPrecedent);
}

public record ClicsTerrainVm(int Id, string Reference, string Titre, StatutTerrain Statut, int Clics, int Fiche, int Liste, int Flottant);

// ---------------------------------------------------------------------------
// Journal des actions
// ---------------------------------------------------------------------------
public class FiltreJournal
{
    public string? Q { get; set; }
    public string? Utilisateur { get; set; }
    public TypeAction? Action { get; set; }
    public string? Entite { get; set; }
    /// <summary>Dates au format yyyy-MM-dd (heure de Dakar).</summary>
    public DateOnly? Du { get; set; }
    public DateOnly? Au { get; set; }
    public int Page { get; set; } = 1;

    public int NombreFiltres =>
        (string.IsNullOrEmpty(Utilisateur) ? 0 : 1) + (Action is null ? 0 : 1) + (string.IsNullOrEmpty(Entite) ? 0 : 1)
        + (Du is null ? 0 : 1) + (Au is null ? 0 : 1);
}

public class JournalLigneVm
{
    public long Id { get; init; }
    public DateTime Date { get; init; }
    public string? UtilisateurNom { get; init; }
    public TypeAction Action { get; init; }
    public required string EntiteType { get; init; }
    public string? EntiteId { get; init; }
    public required string Description { get; init; }
    public string? DetailsJson { get; init; }
    /// <summary>Lien vers l'élément concerné dans l'admin, s'il existe encore.</summary>
    public string? Lien { get; set; }
    /// <summary>Ex. « YK-0001 » pour une photo ou un document de terrain.</summary>
    public string? Contexte { get; set; }
}

public class JournalVm
{
    public required FiltreJournal Filtre { get; init; }
    public required PageResultat<JournalLigneVm> Resultats { get; init; }
    public required IReadOnlyList<(string Id, string Nom)> Utilisateurs { get; init; }
    public required IReadOnlyList<(string Cle, string Libelle)> Entites { get; init; }
}
