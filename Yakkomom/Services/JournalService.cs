using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Data;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

/// <summary>
/// Utilise son propre DbContext : écrire au journal ne doit jamais enregistrer, par effet de bord,
/// d'autres modifications en attente dans le contexte de la requête.
/// </summary>
public class JournalService(DbContextOptions<YakkomomDbContext> options, IHttpContextAccessor http, ILogger<JournalService> logger) : IJournalService
{
    public Task EnregistrerAsync(TypeAction action, string entiteType, string? entiteId, string description,
        object? details = null, CancellationToken ct = default)
    {
        var utilisateur = http.HttpContext?.User;
        return EnregistrerPourAsync(
            utilisateur?.FindFirstValue(ClaimTypes.NameIdentifier),
            utilisateur?.Identity?.Name,
            action, entiteType, entiteId, description, details, ct);
    }

    public async Task EnregistrerPourAsync(string? utilisateurId, string? utilisateurNom, TypeAction action,
        string entiteType, string? entiteId, string description, object? details = null, CancellationToken ct = default)
    {
        await using var db = new YakkomomDbContext(options);
        db.JournalActions.Add(new JournalAction
        {
            UtilisateurId = utilisateurId,
            UtilisateurNom = utilisateurNom,
            Action = action,
            EntiteType = entiteType,
            EntiteId = entiteId,
            Description = description.Length > 500 ? description[..500] : description,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details)
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Le journal ne doit jamais faire échouer l'action elle-même.
            logger.LogError(ex, "Écriture du journal impossible ({Action} {Entite} {Id}).", action, entiteType, entiteId);
        }
    }
}
