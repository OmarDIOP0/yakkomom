using Microsoft.AspNetCore.Identity;

namespace Yakkomom.Securite;

/// <summary>Messages d'erreur d'ASP.NET Core Identity en français.</summary>
public class ErreursIdentiteFr : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => E(nameof(DefaultError), "Une erreur inconnue est survenue.");
    public override IdentityError ConcurrencyFailure() => E(nameof(ConcurrencyFailure), "Le compte a été modifié entre-temps. Rechargez la page.");
    public override IdentityError PasswordMismatch() => E(nameof(PasswordMismatch), "Mot de passe incorrect.");
    public override IdentityError InvalidToken() => E(nameof(InvalidToken), "Lien ou jeton invalide.");
    public override IdentityError InvalidEmail(string? email) => E(nameof(InvalidEmail), $"L'adresse e-mail « {email} » n'est pas valide.");
    public override IdentityError DuplicateEmail(string email) => E(nameof(DuplicateEmail), $"L'adresse e-mail « {email} » est déjà utilisée.");
    public override IdentityError InvalidUserName(string? userName) => E(nameof(InvalidUserName), $"L'identifiant « {userName} » n'est pas valide.");
    public override IdentityError DuplicateUserName(string userName) => E(nameof(DuplicateUserName), $"L'identifiant « {userName} » est déjà utilisé.");
    public override IdentityError UserAlreadyInRole(string role) => E(nameof(UserAlreadyInRole), $"Ce compte a déjà le rôle {role}.");
    public override IdentityError UserNotInRole(string role) => E(nameof(UserNotInRole), $"Ce compte n'a pas le rôle {role}.");
    public override IdentityError PasswordTooShort(int length) => E(nameof(PasswordTooShort), $"Le mot de passe doit contenir au moins {length} caractères.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => E(nameof(PasswordRequiresUniqueChars), $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => E(nameof(PasswordRequiresNonAlphanumeric), "Le mot de passe doit contenir au moins un caractère spécial.");
    public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit), "Le mot de passe doit contenir au moins un chiffre.");
    public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower), "Le mot de passe doit contenir au moins une minuscule.");
    public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper), "Le mot de passe doit contenir au moins une majuscule.");

    private static IdentityError E(string code, string description) => new() { Code = code, Description = description };
}
