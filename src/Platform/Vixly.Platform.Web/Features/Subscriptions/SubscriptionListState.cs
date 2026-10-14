using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: SubscriptionListState.cs
//
// PURPOSE:
// State of the subscriptions list.
//
// RESPONSIBILITIES:
// - Hold one page of subscriptions
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record SubscriptionListState(
    IReadOnlyList<PlatformSubscriptionListItem> Subscriptions,
    int Page,
    int TotalPages,
    int TotalCount,
    bool Loaded,
    Error? Error)
{
    public static SubscriptionListState Initial { get; } = new(Array.Empty<PlatformSubscriptionListItem>(), 1, 0, 0, false, null);
}
