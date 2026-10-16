using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Licensing;

// =========================================================
// FILE: PlatformDeviceChallengeConfiguration.cs
//
// PURPOSE:
// Maps key-pair challenges (D-L5).
//
// RESPONSIBILITIES:
// - Map to DeviceChallenges with a unique hash
//
// IMPORTANT NOTES:
// - Single use, 2 minutes
// =========================================================
internal sealed class PlatformDeviceChallengeConfiguration : IEntityTypeConfiguration<PlatformDeviceChallenge>
{
    public void Configure(EntityTypeBuilder<PlatformDeviceChallenge> builder)
    {
        builder.ToTable("DeviceChallenges");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ChallengeHash).HasMaxLength(PlatformLicensingLimits.HashLength).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.ChallengeHash).IsUnique();
        builder.HasOne<PlatformDevice>().WithMany().HasForeignKey(item => item.PlatformDeviceId).OnDelete(DeleteBehavior.Restrict);
    }
}
