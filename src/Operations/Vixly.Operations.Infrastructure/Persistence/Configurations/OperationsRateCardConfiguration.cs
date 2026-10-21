using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsRateCardConfiguration.cs
//
// PURPOSE:
// Maps OperationsRateCard (a price list) to the database (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Columns, the JSON versions with their bands and items, indexes and links
//
// IMPORTANT NOTES:
// - One price list per service, zone and customer: NULL counts as one value in the unique index, so one standard list for all zones
// =========================================================
internal sealed class OperationsRateCardConfiguration : IEntityTypeConfiguration<OperationsRateCard>
{
    public void Configure(EntityTypeBuilder<OperationsRateCard> builder)
    {
        builder.ToTable("RateCards");
        builder.HasKey(card => card.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(card => card.ServiceCode).HasMaxLength(OperationsChargeLimits.ServiceCodeMaxLength).IsUnicode(false).IsRequired();
        builder.OwnsMany(card => card.Versions, version =>
        {
            version.ToJson("Versions");
            version.Property(item => item.MinimumCharge).HasPrecision(12, 2);
            version.Property(item => item.ServiceFee).HasPrecision(12, 2);
            version.Property(item => item.ConsolidationFee).HasPrecision(12, 2);
            version.Property(item => item.PerPackage).HasPrecision(12, 2);
            version.Property(item => item.PerShipment).HasPrecision(12, 2);
            version.OwnsMany(item => item.Bands, band =>
            {
                band.Property(value => value.From).HasPrecision(12, 2);
                band.Property(value => value.Amount).HasPrecision(14, 4);
                band.Property(value => value.Base).HasPrecision(12, 2);
            });
            version.OwnsMany(item => item.Items, rateItem => rateItem.Property(value => value.Price).HasPrecision(12, 2));
            version.Navigation(item => item.Bands).UsePropertyAccessMode(PropertyAccessMode.Field);
            version.Navigation(item => item.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Navigation(card => card.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(card => card.Customs).HasDefaultValue(OperationsCustomsPolicy.TransportOnly).HasSentinel((OperationsCustomsPolicy)0);

        // One list per service, route and customer; "any" (null) counts as a value.
        builder.HasIndex(card => new { card.TenantId, card.ServiceCode, card.OriginWarehouseId, card.DestinationWarehouseId, card.CustomerId }).IsUnique().HasFilter(null);
        builder.HasIndex(card => new { card.TenantId, card.CustomerId });
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(card => card.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(card => card.OriginWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(card => card.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
