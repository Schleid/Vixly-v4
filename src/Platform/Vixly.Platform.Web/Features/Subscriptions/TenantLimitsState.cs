using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantLimitsState.cs
//
// PURPOSE:
// State of the limits section (D-S8).
//
// RESPONSIBILITIES:
// - Hold the limit being overridden, a pending removal, step-up and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantLimitsState(PlatformPlanLimitKind? Editing, Guid? Removing, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TenantLimitsState Initial { get; } = new(null, null, false, null, null);
}
