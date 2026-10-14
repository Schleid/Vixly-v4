using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantLifecycleState.cs
//
// PURPOSE:
// State of the lifecycle panel.
//
// RESPONSIBILITIES:
// - Hold the move being confirmed, the step-up prompt and the
//   outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantLifecycleState(TenantMove? Confirming, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TenantLifecycleState Initial { get; } = new(null, false, null, null);
}
