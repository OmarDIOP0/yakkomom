using Microsoft.AspNetCore.Identity;
using Yakkomom.Models.Entities;
using Yakkomom.Securite;

namespace Yakkomom.Data.Seed;

/// <summary>
/// Crée les rôles, puis le premier SuperAdmin à partir de la configuration
/// (variables d'environnement <c>AdminInitial__Email</c>, <c>AdminInitial__MotDePasse</c>,
/// <c>AdminInitial__Nom</c>). Ne fait rien s'il existe déjà un SuperAdmin et
/// ne modifie jamais un mot de passe existant.
/// </summary>
public class AdminInitialSeeder(
    RoleManager<IdentityRole> roles,
    UserManager<ApplicationUser> utilisateurs,
    IConfiguration configuration,
    ILogger<AdminInitialSeeder> logger)
{
    public async Task SeedAsync()
    {
        foreach (var role in Roles.Tous)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        if ((await utilisateurs.GetUsersInRoleAsync(Roles.SuperAdmin)).Count > 0) return;

        var email = configuration["AdminInitial:Email"]?.Trim();
        var motDePasse = configuration["AdminInitial:MotDePasse"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(motDePasse))
        {
            logger.LogWarning("Aucun SuperAdmin : définissez AdminInitial__Email et AdminInitial__MotDePasse pour créer le premier compte.");
            return;
        }

        var compte = await utilisateurs.FindByEmailAsync(email);
        if (compte is null)
        {
            compte = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NomComplet = configuration["AdminInitial:Nom"] ?? "Administrateur",
                LockoutEnabled = true
            };
            var creation = await utilisateurs.CreateAsync(compte, motDePasse);
            if (!creation.Succeeded)
            {
                logger.LogError("Création du SuperAdmin impossible : {Erreurs}",
                    string.Join(" ", creation.Errors.Select(e => e.Description)));
                return;
            }
        }

        await utilisateurs.AddToRoleAsync(compte, Roles.SuperAdmin);
        logger.LogInformation("SuperAdmin initial créé : {Email}.", email);
    }
}
