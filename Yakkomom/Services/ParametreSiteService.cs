using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Yakkomom.Data;
using Yakkomom.Models.Entities;
using Yakkomom.Models.Enums;
using Yakkomom.Models.ViewModels.Admin;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

public class ParametreSiteService(YakkomomDbContext db, IMemoryCache cache, IJournalService journal) : IParametreSiteService
{
    private const string CleCache = "parametres-site";

    public async Task<ParametreSite> ObtenirAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CleCache, out ParametreSite? enCache) && enCache is not null)
            return enCache;

        var parametres = await db.ParametresSite.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == ParametreSite.IdUnique, ct) ?? new ParametreSite();

        cache.Set(CleCache, parametres, TimeSpan.FromMinutes(10));
        return parametres;
    }

    public async Task<ResultatOperation> EnregistrerAsync(ParametresVm vm, CancellationToken ct = default)
    {
        var p = await db.ParametresSite.FirstOrDefaultAsync(x => x.Id == ParametreSite.IdUnique, ct);
        if (p is null)
        {
            p = new ParametreSite();
            db.ParametresSite.Add(p);
        }

        var r = ContactsSaisie.Appliquer(p.ContactsParDefaut,
            (vm.WhatsApp1, vm.WhatsApp1Libelle), (vm.WhatsApp2, vm.WhatsApp2Libelle), (vm.WhatsApp3, vm.WhatsApp3Libelle), vm.Email);
        if (!vm.MessageWhatsAppTerrain.Contains("{lien}"))
            r.AjouterErreur(nameof(vm.MessageWhatsAppTerrain), "Le message doit contenir {lien} pour que le terrain soit identifiable.");
        if (!r.Reussi) return r;

        p.NomSite = vm.NomSite.Trim();
        p.Slogan = Nettoyer(vm.Slogan);
        p.MessageWhatsAppTerrain = vm.MessageWhatsAppTerrain.Trim();
        p.Adresse = Nettoyer(vm.Adresse);
        p.HorairesOuverture = Nettoyer(vm.HorairesOuverture);
        p.Facebook = Nettoyer(vm.Facebook);
        p.Instagram = Nettoyer(vm.Instagram);
        p.TikTok = Nettoyer(vm.TikTok);
        p.YouTube = Nettoyer(vm.YouTube);
        p.LinkedIn = Nettoyer(vm.LinkedIn);
        p.ModifieLe = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        InvaliderCache();
        await journal.EnregistrerAsync(TypeAction.Modification, nameof(ParametreSite), p.Id.ToString(), "Modification des paramètres du site", ct: ct);
        return r;
    }

    public void InvaliderCache() => cache.Remove(CleCache);

    private static string? Nettoyer(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
