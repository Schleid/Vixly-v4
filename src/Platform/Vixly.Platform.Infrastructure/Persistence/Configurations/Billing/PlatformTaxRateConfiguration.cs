using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformTaxRateConfiguration.cs
//
// PURPOSE:
// Maps tax rates (D-B1).
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - Never updated; a new rate is a new row
// =========================================================
internal sealed class PlatformTaxRateConfiguration : IEntityTypeConfiguration<PlatformTaxRate>
{
    public void Configure(EntityTypeBuilder<PlatformTaxRate> builder)
    {
        builder.ToTable("TaxRates");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.RegionCode).HasMaxLength(PlatformBillingLimits.RegionMaxLength);
        builder.Property(item => item.Rate).HasPrecision(9, 4);
        builder.Property(item => item.Label).HasMaxLength(PlatformBillingLimits.TaxLabelMaxLength).IsRequired();
        builder.HasIndex(item => new { item.CountryCode, item.RegionCode, item.ValidFromUtc });
        builder.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
