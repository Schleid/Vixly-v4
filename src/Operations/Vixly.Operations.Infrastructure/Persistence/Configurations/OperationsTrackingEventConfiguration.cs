using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsTrackingEventConfiguration.cs
//
// PURPOSE:
// Maps tracking events to ops.TrackingEvents (Slice 3A, ADR-0021).
//
// RESPONSIBILITIES:
// - One idempotency key per tenant
// - Read a subject's history in order
//
// IMPORTANT NOTES:
// - Append-only: the app login may only insert and read (global SQL script)
// =========================================================
internal sealed class OperationsTrackingEventConfiguration : IEntityTypeConfiguration<OperationsTrackingEvent>
{
    public void Configure(EntityTypeBuilder<OperationsTrackingEvent> builder)
    {
        builder.ToTable("TrackingEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(OperationsTrackingEvent.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ArgumentsJson).HasMaxLength(OperationsTrackingEvent.ArgumentsMaxLength);
        builder.Property(item => item.TimeZone).HasMaxLength(OperationsTrackingEvent.TimeZoneMaxLength).IsUnicode(false);
        builder.Property(item => item.LocationCode).HasMaxLength(OperationsTrackingEvent.LocationCodeMaxLength).IsUnicode(false);
        builder.Property(item => item.IdempotencyKey).HasMaxLength(OperationsTrackingEvent.IdempotencyKeyMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Note).HasMaxLength(OperationsTrackingEvent.NoteMaxLength);
        builder.HasIndex(item => new { item.TenantId, item.IdempotencyKey }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.SubjectKind, item.SubjectId, item.OccurredUtc });
        builder.HasIndex(item => new { item.TenantId, item.VoidsEventId }).HasFilter("[VoidsEventId] IS NOT NULL").IsUnique();

        // Slice 6C: the copies of an event on a shipment, bundle or dispatch.
        builder.HasIndex(item => new { item.TenantId, item.CausedByEventId }).HasFilter("[CausedByEventId] IS NOT NULL");
    }
}
