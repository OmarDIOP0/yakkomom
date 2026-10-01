namespace Yakkomom.Services;

/// <summary>Résultat d'une opération métier, avec erreurs par champ (pour ModelState).</summary>
public class ResultatOperation
{
    private readonly Dictionary<string, string> _erreurs = [];

    public bool Reussi => _erreurs.Count == 0 && Erreur is null;
    public string? Erreur { get; private set; }
    /// <summary>Message de succès facultatif (ex. « 40 lots ajoutés. »).</summary>
    public string? Message { get; set; }
    public IReadOnlyDictionary<string, string> Erreurs => _erreurs;

    public ResultatOperation AjouterErreur(string champ, string message)
    {
        _erreurs.TryAdd(champ, message);
        return this;
    }

    public static ResultatOperation Ok() => new();
    public static ResultatOperation Echec(string message) => new() { Erreur = message };
}
