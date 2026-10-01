using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yakkomom.Models.Entities;

namespace Yakkomom.Data.Configurations;

public class TerrainConfiguration : IEntityTypeConfiguration<Terrain>
{
    public void Configure(EntityTypeBuilder<Terrain> b)
    {
        b.ToTable("terrains");

        b.Property(t => t.Reference).HasMaxLength(20).IsRequired();
        b.HasIndex(t => t.Reference).IsUnique();

        b.Property(t => t.Titre).HasMaxLength(200).IsRequired();
        b.Property(t => t.QuartierVillage).HasMaxLength(120);
        b.Property(t => t.Adresse).HasMaxLength(300);
        b.Property(t => t.RouteAccesDetail).HasMaxLength(200);
        b.Property(t => t.VideoYoutubeUrl).HasMaxLength(300);
        b.Property(t => t.ContourGeoJson).HasColumnType("jsonb");
        b.Property(t => t.CreeParId).HasMaxLength(450);
        b.Property(t => t.ModifieParId).HasMaxLength(450);

        b.Ignore(t => t.EstPublic);

        b.OwnsMany(t => t.Commodites, c =>
        {
            c.ToJson("commodites");
            c.Property(x => x.Libelle).HasMaxLength(100);
        });

        b.OwnsOne(t => t.Contacts, c => c.ConfigurerColonnes());
        b.Navigation(t => t.Contacts).IsRequired();

        b.HasOne(t => t.Region).WithMany().HasForeignKey(t => t.RegionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Departement).WithMany().HasForeignKey(t => t.DepartementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Commune).WithMany().HasForeignKey(t => t.CommuneId).OnDelete(DeleteBehavior.Restrict);

        // Index utiles aux filtres publics
        b.HasIndex(t => t.Statut);
        b.HasIndex(t => t.Type);
        b.HasIndex(t => t.Prix);
        b.HasIndex(t => t.SurfaceM2);
        b.HasIndex(t => new { t.EstMisEnAvant, t.OrdreMiseEnAvant });
        b.HasIndex(t => t.PublieLe);
    }
}

public class TerrainPhotoConfiguration : IEntityTypeConfiguration<TerrainPhoto>
{
    public void Configure(EntityTypeBuilder<TerrainPhoto> b)
    {
        b.ToTable("terrain_photos");
        b.Property(p => p.CleStockage).HasMaxLength(300).IsRequired();
        b.Property(p => p.CleVignette).HasMaxLength(300);
        b.Property(p => p.TypeMime).HasMaxLength(50).IsRequired();
        b.Property(p => p.CouleurDominante).HasMaxLength(7);
        b.Property(p => p.Legende).HasMaxLength(200);
        b.HasIndex(p => new { p.TerrainId, p.IdEnvoi }).IsUnique().HasFilter("id_envoi IS NOT NULL");

        b.HasOne(p => p.Terrain).WithMany(t => t.Photos).HasForeignKey(p => p.TerrainId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => new { p.TerrainId, p.Ordre });
        // Une seule photo de couverture par terrain
        b.HasIndex(p => p.TerrainId).IsUnique().HasFilter("est_couverture").HasDatabaseName("ix_terrain_photos_couverture_unique");
    }
}

public class DocumentFoncierConfiguration : IEntityTypeConfiguration<DocumentFoncier>
{
    public void Configure(EntityTypeBuilder<DocumentFoncier> b)
    {
        b.ToTable("documents_fonciers");
        b.Property(d => d.Titre).HasMaxLength(200);
        b.Property(d => d.Notes).HasMaxLength(1000);
        b.Property(d => d.CleStockage).HasMaxLength(300).IsRequired();
        b.Property(d => d.NomFichierOriginal).HasMaxLength(255).IsRequired();
        b.Property(d => d.TypeMime).HasMaxLength(100).IsRequired();
        b.Property(d => d.CreeParId).HasMaxLength(450);
        b.Property(d => d.EstPublic).HasDefaultValue(false);
        b.HasIndex(d => new { d.TerrainId, d.IdEnvoi }).IsUnique().HasFilter("id_envoi IS NOT NULL");

        b.HasOne(d => d.Terrain).WithMany(t => t.Documents).HasForeignKey(d => d.TerrainId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(d => new { d.TerrainId, d.Type });
    }
}

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> b)
    {
        b.ToTable("lots");
        b.Property(l => l.Numero).HasMaxLength(20).IsRequired();
        b.Property(l => l.SurfaceM2).HasPrecision(12, 2);
        b.Property(l => l.Position).HasMaxLength(120);
        b.Ignore(l => l.PrixEffectif);
        b.Ignore(l => l.PrixM2Effectif);
        b.HasOne(l => l.Terrain).WithMany(t => t.Lots).HasForeignKey(l => l.TerrainId).OnDelete(DeleteBehavior.Cascade);
        // Un numéro de lot n'apparaît qu'une fois par lotissement
        b.HasIndex(l => new { l.TerrainId, l.Numero }).IsUnique();
        b.HasIndex(l => new { l.TerrainId, l.Statut });
    }
}

public class PanoramaConfiguration : IEntityTypeConfiguration<Panorama>
{
    public void Configure(EntityTypeBuilder<Panorama> b)
    {
        b.ToTable("panoramas");
        b.Property(p => p.Titre).HasMaxLength(120);
        b.Property(p => p.CleStockageHd).HasMaxLength(300).IsRequired();
        b.Property(p => p.CleStockageBd).HasMaxLength(300);
        b.Property(p => p.CleVignette).HasMaxLength(300);

        b.HasOne(p => p.Terrain).WithMany(t => t.Panoramas).HasForeignKey(p => p.TerrainId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => new { p.TerrainId, p.Ordre });
        b.HasIndex(p => p.TerrainId).IsUnique().HasFilter("est_depart").HasDatabaseName("ix_panoramas_depart_unique");
        b.HasIndex(p => new { p.TerrainId, p.IdEnvoi }).IsUnique().HasFilter("id_envoi IS NOT NULL");
        b.Property(p => p.Vaov).HasDefaultValue(180d);
    }
}

public class HotspotConfiguration : IEntityTypeConfiguration<Hotspot>
{
    public void Configure(EntityTypeBuilder<Hotspot> b)
    {
        b.ToTable("hotspots");
        b.Property(h => h.Texte).HasMaxLength(200);

        b.HasOne(h => h.Panorama).WithMany(p => p.Hotspots).HasForeignKey(h => h.PanoramaId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(h => h.PanoramaCible).WithMany().HasForeignKey(h => h.PanoramaCibleId).OnDelete(DeleteBehavior.Cascade);
    }
}
