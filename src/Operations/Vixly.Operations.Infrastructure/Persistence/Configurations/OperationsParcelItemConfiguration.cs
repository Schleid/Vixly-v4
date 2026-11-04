using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Items;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsParcelItemConfiguration.cs
//
// PURPOSE:
// Maps the items of parcels and pre-alerts (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Table ParcelItems, found by parcel and by pre-alert
//
// IMPORTANT NOTES:
// - Tenant filter and audit columns come from the model conventions
// - Customs code digits only; country of origin ISO-2
// =========================================================
internal sealed class OperationsParcelItemConfiguration : IEntityTypeConfiguration<OperationsParcelItem>
{
    public void Configure(EntityTypeBuilder<OperationsParcelItem> builder)
    {
        builder.ToTable("ParcelItems");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Words).HasMaxLength(OperationsParcelItem.WordsMaxLength).IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(12, 3);
        builder.Property(item => item.ValueEach).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.Description).HasMaxLength(OperationsParcelItem.DescriptionMaxLength);
        builder.Property(item => item.HsCode).HasMaxLength(OperationsHsCode.MaxDigits).IsUnicode(false);
        builder.Property(item => item.OriginCountryCode).HasMaxLength(2).IsUnicode(false).IsFixedLength();
        builder.Ignore(item => item.IsComplete);
        builder.Ignore(item => item.Total);
        builder.HasIndex(item => new { item.TenantId, item.ParcelId });
        builder.HasIndex(item => new { item.TenantId, item.PreAlertId });
    }
}
