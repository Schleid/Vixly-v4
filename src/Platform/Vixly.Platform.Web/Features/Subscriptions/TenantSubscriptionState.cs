using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionState.cs
//
// PURPOSE:
// State of a tenant's subscription section.
//
// RESPONSIBILITIES:
// - Hold the subscription, abilities, plan options, limits, the open form, the quote and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - Version changes after every reload so child sections refresh
// =========================================================
public sealed record TenantSubscriptionState(
    PlatformTenantSubscriptionDetail? Subscription,
    PlatformSubscriptionAbilities Abilities,
    IReadOnlyList<PlatformPlanOption> PlanOptions,
    IReadOnlyList<PlatformLimitItem> Limits,
    bool Loaded,
    int Version,
    TenantSubscriptionMode Mode,
    PlatformSubscriptionQuoteItem? Quote,
    string? DoneKey,
    Error? Error)
{
    public static TenantSubscriptionState Initial { get; } = new(
        null,
        PlatformSubscriptionAbilities.None,
        Array.Empty<PlatformPlanOption>(),
        Array.Empty<PlatformLimitItem>(),
        false,
        0,
        TenantSubscriptionMode.None,
        null,
        null,
        null);
}
