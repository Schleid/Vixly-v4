using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformSubscriptionListFilter.cs
//
// PURPOSE:
// Filters of the subscriptions list.
//
// RESPONSIBILITIES:
// - Carry status, plan, renewal window, search text and the reader language
//
// IMPORTANT NOTES:
// - Built by the use case from validated input
// =========================================================
public sealed record PlatformSubscriptionListFilter(
    PlatformSubscriptionStatus? Status,
    Guid? PlatformPlanId,
    DateTimeOffset? RenewsBeforeUtc,
    string? Search,
    bool IncludeEnded,
    bool DiscountedOnly,
    string Language);
