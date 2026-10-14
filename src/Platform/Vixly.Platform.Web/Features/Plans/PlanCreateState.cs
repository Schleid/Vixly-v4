using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanCreateState.cs
//
// PURPOSE:
// State of the new plan form.
//
// RESPONSIBILITIES:
// - Hold the outcome of creating a plan
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record PlanCreateState(Guid? CreatedId, Error? Error)
{
    public static PlanCreateState Initial { get; } = new(null, null);
}
