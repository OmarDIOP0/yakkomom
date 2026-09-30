using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Yakkomom.Securite;

namespace Yakkomom.Areas.Admin.Controllers;

/// <summary>
/// Base de tous les contrôleurs admin : authentification obligatoire (rôle Admin ou SuperAdmin),
/// jeton anti-falsification sur chaque POST, pages exclues des moteurs de recherche et
/// changement du mot de passe provisoire imposé avant toute autre action.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Politiques.EspaceAdmin)]
[AutoValidateAntiforgeryToken]
public abstract class AdminControllerBase : Controller
{
    public const string CleMessage = "yk.message";
    public const string CleMessageType = "yk.message.type";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers.CacheControl = "no-store";

        var doitChanger = User.HasClaim(c => c.Type == ConfigurationSecurite.ClaimChangementMotDePasse);
        var surPageAutorisee = context.ActionDescriptor.EndpointMetadata.OfType<AutoriseAvecMotDePasseProvisoireAttribute>().Any();
        if (doitChanger && !surPageAutorisee)
            context.Result = RedirectToAction("MotDePasse", "Compte", new { area = "Admin" });

        base.OnActionExecuting(context);
    }

    protected void Succes(string message) => Message(message, "succes");
    protected void Erreur(string message) => Message(message, "erreur");
    protected void Info(string message) => Message(message, "info");

    private void Message(string message, string type)
    {
        TempData[CleMessage] = message;
        TempData[CleMessageType] = type;
    }
}

/// <summary>Action accessible même si le mot de passe provisoire n'a pas encore été changé.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AutoriseAvecMotDePasseProvisoireAttribute : Attribute;
