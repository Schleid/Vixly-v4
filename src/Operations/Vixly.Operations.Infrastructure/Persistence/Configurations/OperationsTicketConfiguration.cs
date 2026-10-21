using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsTicketConfiguration.cs
//
// PURPOSE:
// Maps the customers' tickets about missing packages to ops.Tickets (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - The messages as JSON inside the ticket; indexes for the customer's list and the warehouse's queue
//
// IMPORTANT NOTES:
// - A row version, so a customer and staff writing at once never overwrite each other
// =========================================================
internal sealed class OperationsTicketConfiguration : IEntityTypeConfiguration<OperationsTicket>
{
    public void Configure(EntityTypeBuilder<OperationsTicket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Carrier).HasMaxLength(OperationsReceivingLimits.CarrierMaxLength).IsRequired();
        builder.Property(item => item.TrackingNumber).HasMaxLength(OperationsReceivingLimits.TrackingNumberMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Store).HasMaxLength(OperationsReceivingLimits.StoreMaxLength);
        builder.Property(item => item.OrderNumber).HasMaxLength(60);
        builder.Property(item => item.Contents).HasMaxLength(OperationsReceivingLimits.ContentsMaxLength).IsRequired();
        builder.Property(item => item.ProofKey).HasMaxLength(400);
        builder.Property(item => item.ProofContentType).HasMaxLength(100).IsUnicode(false);
        builder.Property(item => item.ProofSha256).HasMaxLength(64).IsUnicode(false);
        builder.Property(item => item.LastCommandKey).HasMaxLength(60).IsUnicode(false);
        builder.OwnsMany(item => item.Messages, message =>
        {
            message.ToJson("Messages");
            message.Property(item => item.Text).HasMaxLength(OperationsTicket.TextMaxLength);
        });
        builder.Navigation(item => item.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.CreatedUtc });
        // One ticket not yet resolved per customer and package (a report sent twice opens the first).
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.TrackingNumber }).IsUnique().HasFilter("[Status] <> 3");
        builder.HasIndex(item => new { item.TenantId, item.WarehouseId, item.Status, item.CreatedUtc });
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
