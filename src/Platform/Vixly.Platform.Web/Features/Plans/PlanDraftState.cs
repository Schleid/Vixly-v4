using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanDraftState.cs
//
// PURPOSE:
// State of the plan versions section.
//
// RESPONSIBILITIES:
// - Hold step-up, the price preview and the outcome of the last action
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record PlanDraftState(bool StepUpPending, string? Preview, string? DoneKey, Error? Error)
{
    public static PlanDraftState Initial { get; } = new(false, null, null, null);
}
