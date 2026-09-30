using System.Collections.Frozen;
using System.Text.Json;
using Microsoft.AspNetCore.StaticAssets;
using Yakkomom.Services.Interfaces;

namespace Yakkomom.Services;

/// <summary>
/// Résout « css/site.css » en « /css/site.xy469wgme2.css » à partir des points de terminaison
/// créés par MapStaticAssets. Ces URLs avec empreinte sont servies avec
/// « Cache-Control: max-age=31536000, immutable » : le téléphone ne les retélécharge jamais
/// tant que le fichier ne change pas.
/// </summary>
public class AssetsStatiques(EndpointDataSource sources) : IAssetsStatiques
{
    private FrozenDictionary<string, string>? _table;
    private string? _importMapPublique;
    private string? _importMapAdmin;

    public string this[string chemin] => Url(chemin);

    public string Url(string chemin)
    {
        var cle = chemin.TrimStart('~', '/');
        var table = _table ??= Construire();
        return table.TryGetValue(cle, out var route) ? route : "/" + cle;
    }

    public string ImportMap(bool inclureAdmin = false) => inclureAdmin
        ? _importMapAdmin ??= ConstruireImportMap(true)
        : _importMapPublique ??= ConstruireImportMap(false);

    private string ConstruireImportMap(bool inclureAdmin) => JsonSerializer.Serialize(new
    {
        imports = (_table ??= Construire())
            .Where(kv => kv.Key.StartsWith("js/", StringComparison.OrdinalIgnoreCase) && kv.Key.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            .Where(kv => inclureAdmin || !kv.Key.StartsWith("js/admin/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => "/" + kv.Key, kv => kv.Value)
    });

    private FrozenDictionary<string, string> Construire()
    {
        var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in sources.Endpoints)
        {
            var descripteur = endpoint.Metadata.GetMetadata<StaticAssetDescriptor>();
            if (descripteur is null) continue;
            var label = descripteur.Properties.FirstOrDefault(p => p.Name == "label")?.Value;
            if (label is null || descripteur.Route.EndsWith(".gz") || descripteur.Route.EndsWith(".br")) continue;
            table[label] = "/" + descripteur.Route;
        }
        return table.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
