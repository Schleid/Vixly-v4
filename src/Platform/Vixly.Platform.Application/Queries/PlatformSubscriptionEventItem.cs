using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformSubscriptionEventItem.cs
//
// PURPOSE:
// One entry of a subscription history.
//
// RESPONSIBILITIES:
// - Carry the event code, arguments, statuses, reason, actor name and time
//
// IMPORTANT NOTES:
// - Arguments are rendered in the reader language by the console
// =========================================================
public sealed record PlatformSubscriptionEventItem(
    Guid EventId,
    Guid PlatformSubscriptionId,
    string EventCode,
    PlatformSubscriptionStatus? FromStatus,
    PlatformSubscriptionStatus ToStatus,
    string? Reason,
    string? ActorName,
    IReadOnlyDictionary<string, string> Arguments,
    DateTimeOffset OccurredUtc);
