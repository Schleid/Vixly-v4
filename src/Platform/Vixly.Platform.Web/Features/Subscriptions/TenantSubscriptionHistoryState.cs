using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionHistoryState.cs
//
// PURPOSE:
// State of a tenant's subscription history.
//
// RESPONSIBILITIES:
// - Hold one page of history
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantSubscriptionHistoryState(
    IReadOnlyList<PlatformSubscriptionEventItem> Events,
    int Page,
    int TotalPages,
    bool Loaded,
    Error? Error)
{
    public static TenantSubscriptionHistoryState Initial { get; } = new(Array.Empty<PlatformSubscriptionEventItem>(), 1, 0, false, null);
}
