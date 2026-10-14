using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionActionState.cs
//
// PURPOSE:
// State of the subscription actions.
//
// RESPONSIBILITIES:
// - Hold the action being confirmed, step-up and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantSubscriptionActionState(TenantSubscriptionAction? Confirming, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TenantSubscriptionActionState Initial { get; } = new(null, false, null, null);
}
