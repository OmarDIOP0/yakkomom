using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Yakkomom.Data;

/// <summary>
/// Utilisée uniquement par les outils <c>dotnet ef</c>. Permet de générer une migration
/// sans base configurée ; <c>dotnet ef database update</c> utilise la vraie chaîne si elle existe.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<YakkomomDbContext>
{
    public YakkomomDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        string chaine;
        try { chaine = ConnexionBaseDeDonnees.Lire(configuration); }
        catch (InvalidOperationException) { chaine = "Host=localhost;Database=yakkomom;Username=postgres"; }

        var options = new DbContextOptionsBuilder<YakkomomDbContext>()
            .UseNpgsql(chaine)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new YakkomomDbContext(options);
    }
}
