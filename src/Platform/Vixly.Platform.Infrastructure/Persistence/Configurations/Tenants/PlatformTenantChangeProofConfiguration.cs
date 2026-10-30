using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformTenantChangeProofConfiguration.cs
//
// PURPOSE:
// Maps change request proofs to platform.TenantChangeProofs (B12).
//
// RESPONSIBILITIES:
// - Key the proof by its request
// - Bound the content to 10 MB
//
// IMPORTANT NOTES:
// - Read only for the staff download
// =========================================================
internal sealed class PlatformTenantChangeProofConfiguration : IEntityTypeConfiguration<PlatformTenantChangeProof>
{
    public void Configure(EntityTypeBuilder<PlatformTenantChangeProof> builder)
    {
        builder.ToTable("TenantChangeProofs");
        builder.HasKey(item => item.PlatformTenantChangeRequestId);
        builder.Property(item => item.FileName).HasMaxLength(PlatformTenantChangeLimits.FileNameMaxLength).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(PlatformTenantChangeLimits.ContentTypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Content).HasMaxLength(PlatformTenantChangeLimits.ProofMaxBytes).IsRequired();
        builder.Property(item => item.Sha256).HasMaxLength(PlatformTenantChangeLimits.HashLength).IsUnicode(false).IsFixedLength().IsRequired();
    }
}
