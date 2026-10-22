using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsShipmentConfiguration.cs
//
// PURPOSE:
// Maps shipments to ops.Shipments (Slice 3B opens them; Slice 5 books them).
//
// RESPONSIBILITIES:
// - Columns and the one-open-shipment rule
//
// IMPORTANT NOTES:
// - One open shipment without instructions per customer and origin warehouse: the database refuses a second
// - A customer may hold several open shipments with instructions (owner, 2026-10-18)
// - The delivery address is optional; its columns are prefixed DeliveryAddress_
// =========================================================
internal sealed class OperationsShipmentConfiguration : IEntityTypeConfiguration<OperationsShipment>
{
    public void Configure(EntityTypeBuilder<OperationsShipment> builder)
    {
        builder.ToTable("Shipments");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.OriginWarehouseId }).IsUnique().HasFilter("[Status] = 0 AND [DestinationCountryCode] IS NULL");
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.Status });
        // Slice 5A: the shipments desk, by origin, newest first.
        builder.HasIndex(item => new { item.TenantId, item.OriginWarehouseId, item.CreatedUtc });
        builder.Property(item => item.DestinationCountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        builder.HasOne<Vixly.Operations.Domain.Locations.OperationsWarehouse>().WithMany().HasForeignKey(item => item.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.RecipientName).HasMaxLength(OperationsShipmentInstructions.RecipientMaxLength);
        builder.Property(item => item.RequestKey).HasMaxLength(60).IsUnicode(false);
        builder.Property(item => item.Number).HasMaxLength(Vixly.Operations.Domain.Receiving.OperationsReceivingLimits.NumberMaxLength).IsUnicode(false);
        builder.HasIndex(item => new { item.TenantId, item.Number }).IsUnique().HasFilter("[Number] IS NOT NULL");
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.RequestKey }).IsUnique().HasFilter("[RequestKey] IS NOT NULL");
        builder.Property(item => item.ServiceCode).HasMaxLength(Vixly.Operations.Domain.Charges.OperationsChargeLimits.ServiceCodeMaxLength).IsUnicode(false);
        builder.Property(item => item.RecipientPhone).HasMaxLength(OperationsShipmentInstructions.PhoneMaxLength).IsUnicode(false);

        // Slice 5C: the amount when it became final, in the company's currency.
        builder.Property(item => item.BookedTotal).HasPrecision(12, 2);
        builder.Property(item => item.BookedCurrencyCode).HasMaxLength(3).IsUnicode(false);

        // Slice 5D: the customer's signature, kept in the file store.
        builder.Property(item => item.CustomerSignatureKey).HasMaxLength(200).IsUnicode(false);

        // Slice 7 step 2: who may collect, the pickup code's hash.
        builder.Property(item => item.CollectorName).HasMaxLength(OperationsShipmentInstructions.RecipientMaxLength);
        builder.Property(item => item.CollectorPhone).HasMaxLength(OperationsShipmentInstructions.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.PickupCodeProtected).HasMaxLength(1000).IsUnicode(false);
        builder.HasIndex(item => new { item.TenantId, item.PickupRemindersSent, item.PickupReadyUtc }).HasFilter("[PickupReadyUtc] IS NOT NULL AND [PickedUpUtc] IS NULL");

        // Slice 5D: the insurance product chosen; a product in use is never deleted.
        builder.HasOne<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>().WithMany().HasForeignKey(item => item.InsurancePlanId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(item => item.DeliveryAddress, address =>
        {
            address.Property(item => item.Line1).HasColumnName("DeliveryAddress_Line1").HasMaxLength(OperationsLocationLimits.AddressLineMaxLength);
            address.Property(item => item.Line2).HasColumnName("DeliveryAddress_Line2").HasMaxLength(OperationsLocationLimits.AddressLineMaxLength);
            address.Property(item => item.City).HasColumnName("DeliveryAddress_City").HasMaxLength(OperationsLocationLimits.CityMaxLength);
            address.Property(item => item.Region).HasColumnName("DeliveryAddress_Region").HasMaxLength(OperationsLocationLimits.RegionMaxLength);
            address.Property(item => item.PostalCode).HasColumnName("DeliveryAddress_PostalCode").HasMaxLength(OperationsLocationLimits.PostalCodeMaxLength);
            address.Property(item => item.CountryCode).HasColumnName("DeliveryAddress_CountryCode").HasMaxLength(2).IsFixedLength().IsUnicode(false);
        });
        builder.HasOne<OperationsBranch>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.DestinationBranchId })
            .HasPrincipalKey(branch => new { branch.TenantId, branch.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsCustomer>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.CustomerId })
            .HasPrincipalKey(customer => new { customer.TenantId, customer.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
