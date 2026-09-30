using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yakkomom.Models.Entities;

namespace Yakkomom.Data.Configurations;

internal static class ContactConfiguration
{
    /// <summary>Colonnes explicites pour le type possédé <see cref="Contact"/> (whatsapp1, email…).</summary>
    public static void ConfigurerColonnes<TOwner>(this OwnedNavigationBuilder<TOwner, Contact> c) where TOwner : class
    {
        c.Property(x => x.WhatsApp1).HasColumnName("whatsapp1").HasMaxLength(20);
        c.Property(x => x.WhatsApp1Libelle).HasColumnName("whatsapp1_libelle").HasMaxLength(60);
        c.Property(x => x.WhatsApp2).HasColumnName("whatsapp2").HasMaxLength(20);
        c.Property(x => x.WhatsApp2Libelle).HasColumnName("whatsapp2_libelle").HasMaxLength(60);
        c.Property(x => x.WhatsApp3).HasColumnName("whatsapp3").HasMaxLength(20);
        c.Property(x => x.WhatsApp3Libelle).HasColumnName("whatsapp3_libelle").HasMaxLength(60);
        c.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        c.Ignore(x => x.EstVide);
    }
}
