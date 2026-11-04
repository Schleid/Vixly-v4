using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsVendorConfiguration.cs
//
// PURPOSE:
// Maps a vendor the company buys from (Slice 8).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One name per company
// =========================================================
internal sealed class OperationsVendorConfiguration : IEntityTypeConfiguration<OperationsVendor>
{
    public void Configure(EntityTypeBuilder<OperationsVendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(vendor => vendor.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(vendor => vendor.Name).HasMaxLength(OperationsVendor.NameMaxLength).IsRequired();
        builder.Property(vendor => vendor.Contact).HasMaxLength(OperationsVendor.ContactMaxLength);
        builder.HasIndex(vendor => new { vendor.TenantId, vendor.Name }).IsUnique();
    }
}
