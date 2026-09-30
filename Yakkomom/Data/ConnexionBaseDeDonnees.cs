using Npgsql;

namespace Yakkomom.Data;

public static class ConnexionBaseDeDonnees
{
    /// <summary>
    /// Lit la chaîne de connexion : <c>ConnectionStrings:DefaultConnection</c> (format Npgsql)
    /// ou, à défaut, <c>DATABASE_URL</c> (format URI postgres://user:pass@host:port/db fourni
    /// par Render, Neon ou Supabase), convertie au format Npgsql.
    /// </summary>
    public static string Lire(IConfiguration configuration)
    {
        var chaine = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(chaine))
            chaine = configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(chaine))
            throw new InvalidOperationException(
                "Aucune chaîne de connexion PostgreSQL. Définissez ConnectionStrings__DefaultConnection ou DATABASE_URL " +
                "(en développement : dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=…\").");

        return chaine.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
               chaine.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            ? DepuisUri(chaine)
            : chaine;
    }

    public static string DepuisUri(string url)
    {
        var uri = new Uri(url);
        var infos = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(infos[0]),
            Password = infos.Length > 1 ? Uri.UnescapeDataString(infos[1]) : null,
            SslMode = SslMode.Prefer
        };

        // Paramètres de requête éventuels (?sslmode=require…)
        foreach (var paire in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = paire.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<SslMode>(kv[1], ignoreCase: true, out var mode))
                builder.SslMode = mode;
        }
        return builder.ConnectionString;
    }
}
