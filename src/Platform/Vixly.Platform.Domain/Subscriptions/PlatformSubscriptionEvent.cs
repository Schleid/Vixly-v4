using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionEvent.cs
//
// PURPOSE:
// One entry of a subscription's history.
//
// RESPONSIBILITIES:
// - Hold the event code and arguments, the status before and after, the reason, the actor and the time
//
// IMPORTANT NOTES:
// - Append-only: the app login has no UPDATE or DELETE on its table
// - Committed with the change it records (D-2)
// - Created only by PlatformSubscription
// =========================================================
public sealed class PlatformSubscriptionEvent : AuditableEntity
{
    private PlatformSubscriptionEvent()
    {
        EventCode = string.Empty;
        Arguments = new Dictionary<string, string>();
    }

    private PlatformSubscriptionEvent(
        Guid platformSubscriptionId,
        Guid platformTenantId,
        string eventCode,
        PlatformSubscriptionStatus? fromStatus,
        PlatformSubscriptionStatus toStatus,
        string? reason,
        Guid? actorUserId,
        IReadOnlyDictionary<string, string> arguments,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformSubscriptionId = platformSubscriptionId;
        PlatformTenantId = platformTenantId;
        EventCode = eventCode;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
        ActorUserId = actorUserId;
        Arguments = new Dictionary<string, string>(arguments, StringComparer.Ordinal);
    }

    public Guid PlatformSubscriptionId { get; private set; }

    public Guid PlatformTenantId { get; private set; }

    public string EventCode { get; private set; }

    public PlatformSubscriptionStatus? FromStatus { get; private set; }

    public PlatformSubscriptionStatus ToStatus { get; private set; }

    public string? Reason { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public IReadOnlyDictionary<string, string> Arguments { get; private set; }

    internal static PlatformSubscriptionEvent Record(
        PlatformSubscription subscription,
        string eventCode,
        PlatformSubscriptionStatus? fromStatus,
        Guid? actorUserId,
        DateTimeOffset utcNow,
        string? reason = null,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        return new PlatformSubscriptionEvent(
            subscription.Id,
            subscription.PlatformTenantId,
            eventCode,
            fromStatus,
            subscription.Status,
            reason,
            actorUserId,
            arguments ?? new Dictionary<string, string>(),
            utcNow);
    }
}
