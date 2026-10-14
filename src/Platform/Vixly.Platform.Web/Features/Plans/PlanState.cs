using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanState.cs
//
// PURPOSE:
// State of the plan page summary.
//
// RESPONSIBILITIES:
// - Hold the plan, what the reader may do and the outcome of the last action
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record PlanState(
    PlatformPlanDetail? Plan,
    PlatformPlanAbilities Abilities,
    bool Loaded,
    int Version,
    string? DoneKey,
    Error? Error)
{
    public static PlanState Initial { get; } = new(null, PlatformPlanAbilities.None, false, 0, null, null);
}
