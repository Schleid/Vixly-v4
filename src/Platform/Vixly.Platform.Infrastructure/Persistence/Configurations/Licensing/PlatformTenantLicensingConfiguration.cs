using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Licensing;

// =========================================================
// FILE: PlatformTenantLicensingConfiguration.cs
//
// PURPOSE:
// Maps a tenant's seats in use (D-L2).
//
// RESPONSIBILITIES:
// - Map to TenantLicensing, one row per tenant, with a row version
//
// IMPORTANT NOTES:
// - The row version stops two hosts handing out one seat
// =========================================================
internal sealed class PlatformTenantLicensingConfiguration : IEntityTypeConfiguration<PlatformTenantLicensing>
{
    public void Configure(EntityTypeBuilder<PlatformTenantLicensing> builder)
    {
        builder.ToTable("TenantLicensing");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => item.PlatformTenantId).IsUnique();
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
