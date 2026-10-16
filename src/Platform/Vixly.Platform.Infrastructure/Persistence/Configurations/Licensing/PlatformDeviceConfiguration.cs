using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Licensing;

// =========================================================
// FILE: PlatformDeviceConfiguration.cs
//
// PURPOSE:
// Maps devices (D-L3, D-L5).
//
// RESPONSIBILITIES:
// - Map to Devices with a row version
// - Index what the lists, the clock and the allowance count read
//
// IMPORTANT NOTES:
// - Only hashes of secrets are stored; public keys are not secret
// =========================================================
internal sealed class PlatformDeviceConfiguration : IEntityTypeConfiguration<PlatformDevice>
{
    public void Configure(EntityTypeBuilder<PlatformDevice> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.HoldsSeat);
        builder.Ignore(item => item.AwaitsReview);
        builder.Property(item => item.Number).HasMaxLength(PlatformLicensingLimits.NumberMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(PlatformLicensingLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.PublicKey).HasMaxLength(PlatformLicensingLimits.PublicKeyMaxLength).IsUnicode(false);
        builder.Property(item => item.SecretHash).HasMaxLength(PlatformLicensingLimits.HashLength).IsUnicode(false);
        builder.Property(item => item.PreviousSecretHash).HasMaxLength(PlatformLicensingLimits.HashLength).IsUnicode(false);
        builder.Property(item => item.RequestedByReference).HasMaxLength(PlatformLicensingLimits.ActorReferenceMaxLength);
        builder.Property(item => item.RequestedByName).HasMaxLength(PlatformLicensingLimits.ActorNameMaxLength);
        builder.HasIndex(item => new { item.PlatformTenantId, item.Number }).IsUnique();
        builder.HasIndex(item => new { item.PlatformTenantId, item.Status });
        builder.HasIndex(item => item.SecretHash).HasFilter("[SecretHash] IS NOT NULL");
        builder.HasIndex(item => new { item.Status, item.CreatedUtc });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
