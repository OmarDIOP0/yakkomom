using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Yakkomom.Data;
using Yakkomom.Models.Entities;

namespace Yakkomom.Securite;

public static class ConfigurationSecurite
{
    public const string ClaimChangementMotDePasse = "yk:doit_changer_mdp";

    public static IServiceCollection AjouterSecuriteAdmin(this IServiceCollection services, IWebHostEnvironment env)
    {
        // --- Identity (pas d'interface d'inscription : les comptes sont créés par le SuperAdmin) ---
        services.AddIdentity<ApplicationUser, IdentityRole>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.SignIn.RequireConfirmedAccount = false;

                o.Password.RequiredLength = 10;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 4;

                // Verrouillage du compte après 5 échecs pendant 15 minutes
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<YakkomomDbContext>()
            .AddErrorDescriber<ErreursIdentiteFr>()
            .AddClaimsPrincipalFactory<FabriqueClaimsAdmin>()
            .AddDefaultTokenProviders();

        // Un compte bloqué ou dont le mot de passe change est déconnecté sous 5 minutes.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));

        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "yk.admin";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromHours(10);
            o.SlidingExpiration = true;
            o.LoginPath = "/admin/connexion";
            o.LogoutPath = "/admin/deconnexion";
            o.AccessDeniedPath = "/admin/acces-refuse";
            o.ReturnUrlParameter = "retour";
            // Requêtes JavaScript (envois de fichiers) : 401/403 au lieu d'une redirection vers la page de connexion,
            // sinon le navigateur suivrait la redirection et croirait l'envoi réussi.
            o.Events.OnRedirectToLogin = ctx => RepondreOuRediriger(ctx, StatusCodes.Status401Unauthorized);
            o.Events.OnRedirectToAccessDenied = ctx => RepondreOuRediriger(ctx, StatusCodes.Status403Forbidden);
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politiques.EspaceAdmin, p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin, Roles.Admin))
            .AddPolicy(Politiques.SuperAdmin, p => p.RequireAuthenticatedUser().RequireRole(Roles.SuperAdmin));

        // --- Clés de chiffrement persistées en base (disque Render éphémère) ------
        services.AddDataProtection()
            .SetApplicationName("Yakkomom")
            .PersistKeysToDbContext<YakkomomDbContext>();

        // --- Limitation du débit sur la connexion : 10 essais / 5 min par adresse IP --
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Politiques.LimiteConnexion, contexte =>
                RateLimitPartition.GetFixedWindowLimiter(
                    contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));
            o.OnRejected = async (contexte, ct) =>
            {
                contexte.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                await contexte.HttpContext.Response.WriteAsync(
                    "<!doctype html><html lang=\"fr\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\">" +
                    "<title>Trop de tentatives</title><body style=\"font-family:system-ui;padding:2rem;max-width:32rem;margin:auto\">" +
                    "<h1>Trop de tentatives de connexion</h1><p>Par sécurité, patientez quelques minutes avant de réessayer.</p>" +
                    "<p><a href=\"/admin/connexion\">Retour à la connexion</a></p></body></html>", ct);
            };
        });

        // --- Derrière le proxy de Render : vraie IP du client et HTTPS -----------
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Render est le seul proxy : on ne lit que la dernière entrée, ajoutée par lui
            // (un client ne peut donc pas usurper son IP pour contourner la limitation).
            o.ForwardLimit = 1;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        return services;
    }

    private static Task RepondreOuRediriger(Microsoft.AspNetCore.Authentication.RedirectContext<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> ctx, int code)
    {
        if (ctx.Request.Headers.Accept.ToString().Contains("application/json"))
            ctx.Response.StatusCode = code;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }
}

/// <summary>Ajoute au cookie l'indicateur « mot de passe provisoire à changer ».</summary>
public class FabriqueClaimsAdmin(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identite = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrEmpty(user.NomComplet))
            identite.AddClaim(new Claim(ClaimTypes.GivenName, user.NomComplet));
        if (user.DoitChangerMotDePasse)
            identite.AddClaim(new Claim(ConfigurationSecurite.ClaimChangementMotDePasse, "1"));
        return identite;
    }
}
