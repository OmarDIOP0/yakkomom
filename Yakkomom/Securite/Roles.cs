namespace Yakkomom.Securite;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";

    public static readonly string[] Tous = [SuperAdmin, Admin];
}

public static class Politiques
{
    /// <summary>Accès à l'espace admin : rôle Admin ou SuperAdmin.</summary>
    public const string EspaceAdmin = "EspaceAdmin";
    /// <summary>Gestion des comptes et paramètres sensibles.</summary>
    public const string SuperAdmin = "SuperAdmin";
    /// <summary>Limitation de débit sur le formulaire de connexion.</summary>
    public const string LimiteConnexion = "connexion";
}
