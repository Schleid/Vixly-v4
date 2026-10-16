using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Legal;

// =========================================================
// FILE: PlatformTenantTermsAcceptanceConfiguration.cs
//
// PURPOSE:
// Maps Terms acceptances to platform.TenantTermsAcceptances.
//
// RESPONSIBILITIES:
// - One per tenant and version
//
// IMPORTANT NOTES:
// - Append-only in the global script (DENY UPDATE, DELETE)
// =========================================================
internal sealed class PlatformTenantTermsAcceptanceConfiguration : IEntityTypeConfiguration<PlatformTenantTermsAcceptance>
{
    public void Configure(EntityTypeBuilder<PlatformTenantTermsAcceptance> builder)
    {
        builder.ToTable("TenantTermsAcceptances");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Version).HasMaxLength(PlatformLegalLimits.VersionMaxLength).IsRequired();
        builder.Property(item => item.Sha256).HasMaxLength(PlatformLegalLimits.HashLength).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
        builder.Property(item => item.ActorReference).HasMaxLength(PlatformLegalLimits.ActorReferenceMaxLength);
        builder.Property(item => item.ActorName).HasMaxLength(PlatformLegalLimits.ActorNameMaxLength);
        builder.Property(item => item.ActorRole).HasMaxLength(PlatformLegalLimits.RoleMaxLength).IsUnicode(false);
        builder.Property(item => item.ClientIp).HasMaxLength(PlatformLegalLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(item => item.UserAgent).HasMaxLength(PlatformLegalLimits.UserAgentMaxLength);
        builder.Property(item => item.Reason).HasMaxLength(PlatformLegalLimits.ReasonMaxLength);
        builder.Property(item => item.Evidence).HasMaxLength(PlatformLegalLimits.MaxEvidenceMegabytes * 1024 * 1024);
        builder.Property(item => item.EvidenceContentType).HasMaxLength(40).IsUnicode(false);
        builder.HasIndex(item => new { item.PlatformTenantId, item.PlatformLegalDocumentId }).IsUnique();
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformLegalDocument>().WithMany().HasForeignKey(item => item.PlatformLegalDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformTenantRegistration>().WithMany().HasForeignKey(item => item.PlatformTenantRegistrationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
