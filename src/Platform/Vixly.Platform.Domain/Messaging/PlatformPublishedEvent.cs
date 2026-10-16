using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Messaging;

// =========================================================
// FILE: PlatformPublishedEvent.cs
//
// PURPOSE:
// One public event in the feed other planes read (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Keep the type, tenant, payload and the outbox message it came from
//
// IMPORTANT NOTES:
// - Sequence is given by the database and grows; it is the feed cursor
// - One row per outbox message and public type (unique)
// - Kept 30 days
// =========================================================
public sealed class PlatformPublishedEvent : AuditableEntity
{
    public const int TypeMaxLength = 100;

    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    private PlatformPublishedEvent()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    private PlatformPublishedEvent(string type, Guid? tenantId, Guid outboxMessageId, string payload, DateTimeOffset occurredUtc, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Type = type;
        PlatformTenantId = tenantId;
        OutboxMessageId = outboxMessageId;
        Payload = payload;
        OccurredUtc = occurredUtc;
    }

    public long Sequence { get; private set; }

    public string Type { get; private set; }

    public Guid? PlatformTenantId { get; private set; }

    public Guid OutboxMessageId { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    public static PlatformPublishedEvent Create(string type, Guid? tenantId, Guid outboxMessageId, string payload, DateTimeOffset occurredUtc, DateTimeOffset utcNow)
    {
        if (string.IsNullOrEmpty(type) || type.Length > TypeMaxLength || outboxMessageId == Guid.Empty || string.IsNullOrEmpty(payload))
        {
            throw new DomainException(PlatformMessagingErrors.TypeInvalid);
        }

        return new PlatformPublishedEvent(type, tenantId, outboxMessageId, payload, occurredUtc, utcNow);
    }
}
