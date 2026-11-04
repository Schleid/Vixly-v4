using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCardReaderConfiguration.cs
//
// PURPOSE:
// Maps a Stripe Terminal card reader of the company to the database (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - One reader id per company
// =========================================================
internal sealed class OperationsCardReaderConfiguration : IEntityTypeConfiguration<OperationsCardReader>
{
    public void Configure(EntityTypeBuilder<OperationsCardReader> builder)
    {
        builder.ToTable("CardReaders");
        builder.HasKey(reader => reader.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(reader => reader.Label).HasMaxLength(OperationsCardReader.LabelMaxLength).IsRequired();
        builder.Property(reader => reader.StripeReaderId).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(reader => reader.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(reader => new { reader.TenantId, reader.StripeReaderId }).IsUnique();
    }
}
