using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yakkomom.Models.Entities;

namespace Yakkomom.Data.Configurations;

public class LocaliteConfiguration : IEntityTypeConfiguration<Localite>
{
    public void Configure(EntityTypeBuilder<Localite> b)
    {
        b.ToTable("localites");
        b.Property(l => l.Nom).HasMaxLength(120).IsRequired();
        b.Property(l => l.Slug).HasMaxLength(140).IsRequired();

        b.HasOne(l => l.Parent).WithMany(l => l.Enfants).HasForeignKey(l => l.ParentId).OnDelete(DeleteBehavior.Restrict);
        // « Dakar » existe comme région ET comme département : l'unicité se fait par niveau et par parent.
        b.HasIndex(l => new { l.Type, l.ParentId, l.Slug }).IsUnique().AreNullsDistinct(false);
        b.HasIndex(l => l.ParentId);
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("services");
        b.Property(s => s.Titre).HasMaxLength(120).IsRequired();
        b.Property(s => s.Slug).HasMaxLength(140).IsRequired();
        b.Property(s => s.Resume).HasMaxLength(300);
        b.Property(s => s.Icone).HasMaxLength(40);
        b.Property(s => s.CleImageCouverture).HasMaxLength(300);
        b.Property(s => s.MessageWhatsApp).HasMaxLength(500);
        b.HasIndex(s => s.Slug).IsUnique();
        b.HasIndex(s => s.Ordre);
    }
}

public class ServicePhotoConfiguration : IEntityTypeConfiguration<ServicePhoto>
{
    public void Configure(EntityTypeBuilder<ServicePhoto> b)
    {
        b.ToTable("service_photos");
        b.Property(p => p.CleStockage).HasMaxLength(300).IsRequired();
        b.Property(p => p.CleVignette).HasMaxLength(300);
        b.Property(p => p.TypeMime).HasMaxLength(50).IsRequired();
        b.Property(p => p.CouleurDominante).HasMaxLength(7);
        b.Property(p => p.Legende).HasMaxLength(200);
        b.HasIndex(p => new { p.ServiceId, p.IdEnvoi }).IsUnique().HasFilter("id_envoi IS NOT NULL");
        b.HasOne(p => p.Service).WithMany(s => s.Photos).HasForeignKey(p => p.ServiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => new { p.ServiceId, p.Ordre });
    }
}

public class ParametreSiteConfiguration : IEntityTypeConfiguration<ParametreSite>
{
    public void Configure(EntityTypeBuilder<ParametreSite> b)
    {
        b.ToTable("parametres_site");
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.NomSite).HasMaxLength(100).IsRequired();
        b.Property(p => p.Slogan).HasMaxLength(200);
        b.Property(p => p.CleLogo).HasMaxLength(300);
        b.Property(p => p.MessageWhatsAppTerrain).HasMaxLength(500).IsRequired();
        b.Property(p => p.Adresse).HasMaxLength(300);
        b.Property(p => p.HorairesOuverture).HasMaxLength(200);
        foreach (var reseau in new[] { nameof(ParametreSite.Facebook), nameof(ParametreSite.Instagram), nameof(ParametreSite.TikTok), nameof(ParametreSite.YouTube), nameof(ParametreSite.LinkedIn) })
            b.Property(reseau).HasMaxLength(300);

        b.OwnsOne(p => p.ContactsParDefaut, c => c.ConfigurerColonnes());
        b.Navigation(p => p.ContactsParDefaut).IsRequired();
    }
}

public class ClicWhatsAppConfiguration : IEntityTypeConfiguration<ClicWhatsApp>
{
    public void Configure(EntityTypeBuilder<ClicWhatsApp> b)
    {
        b.ToTable("clics_whatsapp");
        // Si un terrain est supprimé, on garde le clic pour les totaux globaux.
        b.HasOne(c => c.Terrain).WithMany(t => t.ClicsWhatsApp).HasForeignKey(c => c.TerrainId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(c => c.Service).WithMany().HasForeignKey(c => c.ServiceId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(c => new { c.TerrainId, c.Date });
        b.HasIndex(c => c.Date);
    }
}

public class JournalActionConfiguration : IEntityTypeConfiguration<JournalAction>
{
    public void Configure(EntityTypeBuilder<JournalAction> b)
    {
        b.ToTable("journal_actions");
        b.Property(j => j.UtilisateurId).HasMaxLength(450);
        b.Property(j => j.UtilisateurNom).HasMaxLength(256);
        b.Property(j => j.EntiteType).HasMaxLength(60).IsRequired();
        b.Property(j => j.EntiteId).HasMaxLength(60);
        b.Property(j => j.Description).HasMaxLength(500).IsRequired();
        b.Property(j => j.DetailsJson).HasColumnType("jsonb");
        b.HasIndex(j => j.Date);
        b.HasIndex(j => new { j.EntiteType, j.EntiteId });
    }
}
