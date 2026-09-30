using Yakkomom.Models.Enums;

namespace Yakkomom.Services.Interfaces;

/// <summary>Journal des actions admin (qui a fait quoi, quand).</summary>
public interface IJournalService
{
    /// <summary>Enregistre une action de l'utilisateur connecté.</summary>
    Task EnregistrerAsync(TypeAction action, string entiteType, string? entiteId, string description,
        object? details = null, CancellationToken ct = default);

    /// <summary>Enregistre une action pour un utilisateur explicite (ex. juste après la connexion).</summary>
    Task EnregistrerPourAsync(string? utilisateurId, string? utilisateurNom, TypeAction action,
        string entiteType, string? entiteId, string description, object? details = null, CancellationToken ct = default);
}
