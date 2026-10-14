using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Plans;

// =========================================================
// FILE: PlanListState.cs
//
// PURPOSE:
// State of the plans list.
//
// RESPONSIBILITIES:
// - Hold one page of plans and what the reader may do
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record PlanListState(
    IReadOnlyList<PlatformPlanListItem> Plans,
    int Page,
    int TotalPages,
    int TotalCount,
    PlatformPlanAbilities Abilities,
    bool Loaded,
    Error? Error)
{
    public static PlanListState Initial { get; } = new(Array.Empty<PlatformPlanListItem>(), 1, 0, 0, PlatformPlanAbilities.None, false, null);
}
