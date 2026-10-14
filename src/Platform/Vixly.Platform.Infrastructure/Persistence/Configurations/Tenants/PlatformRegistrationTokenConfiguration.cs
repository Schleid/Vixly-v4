using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformRegistrationTokenConfiguration.cs
//
// PURPOSE:
// Maps registration verification links to
// platform.RegistrationTokens.
//
// RESPONSIBILITIES:
// - Unique link hash; lookup of unused links per registration
//
// IMPORTANT NOTES:
// - Hash only: the link token is never stored
// =========================================================
internal sealed class PlatformRegistrationTokenConfiguration : IEntityTypeConfiguration<PlatformRegistrationToken>
{
    public void Configure(EntityTypeBuilder<PlatformRegistrationToken> builder)
    {
        builder.ToTable("RegistrationTokens");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash)
            .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.PlatformTenantRegistrationId, item.UsedUtc, item.RevokedUtc });
        builder.HasOne<PlatformTenantRegistration>().WithMany().HasForeignKey(item => item.PlatformTenantRegistrationId).OnDelete(DeleteBehavior.Cascade);
    }
}
