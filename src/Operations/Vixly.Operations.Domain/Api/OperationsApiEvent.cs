using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsApiEvent.cs
//
// PURPOSE:
// One thing that happened, for the company's webhooks (Slice 10; review): written with the change itself, in the same save.
//
// RESPONSIBILITIES:
// - Keep the kind and the record it is about; the database numbers it as it is saved (a rowversion)
//
// IMPORTANT NOTES:
// - Read only up to the oldest save still in progress (MIN_ACTIVE_ROWVERSION), so a slow save is never skipped
// - Kept a week; the record itself is read when the event is sent
// =========================================================
public sealed class OperationsApiEvent : OperationsEntity
{
    public static readonly TimeSpan Keep = TimeSpan.FromDays(7);

    private OperationsApiEvent()
    {
    }

    private OperationsApiEvent(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public OperationsWebhookEvents Kind { get; private set; }

    public Guid SourceId { get; private set; }

    public static OperationsApiEvent Record(Guid tenantId, OperationsWebhookEvents kind, Guid sourceId, DateTimeOffset utcNow) =>
        OperationsWebhookEventNames.All.Contains(kind) ? new OperationsApiEvent(tenantId, utcNow) { Kind = kind, SourceId = sourceId } : throw new ArgumentOutOfRangeException(nameof(kind));
}
