using Yakkomom.Models.Enums;

namespace Yakkomom.Models.ViewModels.Admin;

public class PhotoAdminVm
{
    public int Id { get; init; }
    public required string UrlVignette { get; init; }
    public required string UrlGrande { get; init; }
    public int Largeur { get; init; }
    public int Hauteur { get; init; }
    public long TailleOctets { get; init; }
    public string? CouleurDominante { get; init; }
    public string? Legende { get; init; }
    public int Ordre { get; init; }
    public bool EstCouverture { get; init; }
}

public class PhotosTerrainVm : OngletTerrainVm
{
    public IReadOnlyList<PhotoAdminVm> Photos { get; set; } = [];
}

public class DocumentAdminVm
{
    public int Id { get; init; }
    public TypeDocumentFoncier Type { get; init; }
    public string? Titre { get; init; }
    public required string NomFichier { get; init; }
    public required string TypeMime { get; init; }
    public long TailleOctets { get; init; }
    public bool EstPublic { get; init; }
    public DateTime CreeLe { get; init; }
}

/// <summary>Données envoyées avec une photo (le fichier et la vignette sont lus à part).</summary>
public class EnvoiPhoto
{
    public Guid? IdEnvoi { get; set; }
    public string? CouleurDominante { get; set; }
    public string? Legende { get; set; }
}

public class EnvoiDocument
{
    public Guid? IdEnvoi { get; set; }
    public TypeDocumentFoncier Type { get; set; }
    public string? Titre { get; set; }
    public bool EstPublic { get; set; }
}
