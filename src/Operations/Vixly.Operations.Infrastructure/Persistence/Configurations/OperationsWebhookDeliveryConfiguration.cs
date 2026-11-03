using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsWebhookDeliveryConfiguration.cs
//
// PURPOSE:
// Maps the webhook deliveries (Slice 10).
//
// RESPONSIBILITIES:
// - Columns and indexes; removed with their address
//
// IMPORTANT NOTES:
// - One delivery per event and address; the due ones found by state and time
// =========================================================
internal sealed class OperationsWebhookDeliveryConfiguration : IEntityTypeConfiguration<OperationsWebhookDelivery>
{
    public void Configure(EntityTypeBuilder<OperationsWebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(delivery => delivery.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(delivery => delivery.EventId).HasMaxLength(OperationsWebhookDelivery.EventIdMaxLength).IsUnicode(false).IsRequired();
        builder.Property(delivery => delivery.EventType).HasMaxLength(OperationsWebhookDelivery.EventTypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(delivery => delivery.Payload).IsRequired();
        builder.Property(delivery => delivery.LastError).HasMaxLength(OperationsWebhookDelivery.ErrorMaxLength).IsUnicode(false);
        builder.Ignore(delivery => delivery.IsTest);
        builder.HasOne<OperationsWebhookEndpoint>().WithMany().HasForeignKey(delivery => delivery.EndpointId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(delivery => new { delivery.EndpointId, delivery.EventId }).IsUnique();
        builder.HasIndex(delivery => new { delivery.TenantId, delivery.Status, delivery.NextAttemptUtc });
        builder.HasIndex(delivery => new { delivery.TenantId, delivery.CreatedUtc });
    }
}
