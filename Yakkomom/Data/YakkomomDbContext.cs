using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Yakkomom.Models.Entities;

namespace Yakkomom.Data;

public class YakkomomDbContext(DbContextOptions<YakkomomDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{
    public const string SequenceReferenceTerrain = "terrain_reference_seq";

    public DbSet<Terrain> Terrains => Set<Terrain>();
    public DbSet<TerrainPhoto> TerrainPhotos => Set<TerrainPhoto>();
    public DbSet<Panorama> Panoramas => Set<Panorama>();
    public DbSet<Hotspot> Hotspots => Set<Hotspot>();
    public DbSet<DocumentFoncier> DocumentsFonciers => Set<DocumentFoncier>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServicePhoto> ServicePhotos => Set<ServicePhoto>();
    public DbSet<Localite> Localites => Set<Localite>();
    public DbSet<ParametreSite> ParametresSite => Set<ParametreSite>();
    public DbSet<ClicWhatsApp> ClicsWhatsApp => Set<ClicWhatsApp>();
    public DbSet<JournalAction> JournalActions => Set<JournalAction>();

    /// <summary>Clés de chiffrement des cookies : en base, car le disque de Render est éphémère.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tables Identity nommées comme les autres (snake_case, en français)
        builder.Entity<ApplicationUser>().ToTable("utilisateurs");
        builder.Entity<IdentityRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("utilisateur_roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("utilisateur_claims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("utilisateur_connexions_externes");
        builder.Entity<IdentityUserToken<string>>().ToTable("utilisateur_jetons");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");

        // Séquence servant à générer les références lisibles YK-0001, YK-0002…
        builder.HasSequence<long>(SequenceReferenceTerrain).StartsAt(1).IncrementsBy(1);

        builder.ApplyConfigurationsFromAssembly(typeof(YakkomomDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums stockés en texte : lisibles en base et robustes si l'ordre change.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
        configurationBuilder.Properties<decimal>().HavePrecision(14, 2);
    }
}
