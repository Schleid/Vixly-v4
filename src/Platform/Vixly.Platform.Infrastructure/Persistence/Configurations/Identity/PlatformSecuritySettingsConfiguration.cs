using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformSecuritySettingsConfiguration.cs
//
// PURPOSE:
// Maps Control's sign-in rule to platform.SecuritySettings.
//
// RESPONSIBILITIES:
// - One row by its unique key; who changed it last
//
// IMPORTANT NOTES:
// - Row version guards two administrators saving at once
// =========================================================
internal sealed class PlatformSecuritySettingsConfiguration : IEntityTypeConfiguration<PlatformSecuritySettings>
{
    public void Configure(EntityTypeBuilder<PlatformSecuritySettings> builder)
    {
        builder.ToTable("SecuritySettings");
        builder.HasKey(settings => settings.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(settings => settings.Key).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(settings => settings.Key).IsUnique();
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(settings => settings.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
