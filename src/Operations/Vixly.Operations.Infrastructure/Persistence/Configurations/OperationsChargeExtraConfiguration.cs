using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsChargeExtraConfiguration.cs
//
// PURPOSE:
// Maps OperationsChargeExtra to the database (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Columns, the JSON versions and indexes
//
// IMPORTANT NOTES:
// - One name per company
// =========================================================
internal sealed class OperationsChargeExtraConfiguration : IEntityTypeConfiguration<OperationsChargeExtra>
{
    public void Configure(EntityTypeBuilder<OperationsChargeExtra> builder)
    {
        builder.ToTable("ChargeExtras");
        builder.HasKey(extra => extra.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(extra => extra.Name).HasMaxLength(OperationsChargeLimits.NameMaxLength).IsRequired();
        builder.Property(extra => extra.Words).HasMaxLength(OperationsChargeLimits.WordsMaxLength);
        builder.Property(extra => extra.ServiceCode).HasMaxLength(OperationsChargeLimits.ServiceCodeMaxLength).IsUnicode(false);
        builder.Property(extra => extra.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        builder.OwnsMany(extra => extra.Versions, version =>
        {
            version.ToJson("Versions");
            version.Property(item => item.Amount).HasPrecision(12, 3);
        });
        builder.Navigation(extra => extra.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(extra => new { extra.TenantId, extra.Name }).IsUnique();
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(extra => extra.OriginWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(extra => extra.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
