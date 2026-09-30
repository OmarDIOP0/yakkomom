using Yakkomom.Models.Enums;

namespace Yakkomom.Models.Entities;

/// <summary>Découpage administratif : Région &gt; Département &gt; Commune.</summary>
public class Localite
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TypeLocalite Type { get; set; }

    public int? ParentId { get; set; }
    public Localite? Parent { get; set; }
    public List<Localite> Enfants { get; set; } = [];

    public int Ordre { get; set; }
    public bool EstActive { get; set; } = true;
}
