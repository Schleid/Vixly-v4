using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;

// =========================================================
// FILE: ListPlatformPlansResponse.cs
//
// PURPOSE:
// Pages the plans for the console.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformPlansResponse
{
    private ListPlatformPlansResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformPlanListItem>? Plans { get; private init; }

    public PlatformPlanAbilities Abilities { get; private init; } = PlatformPlanAbilities.None;

    public static ListPlatformPlansResponse Success(PagedResult<PlatformPlanListItem>? plans, PlatformPlanAbilities abilities)
    {
        return new ListPlatformPlansResponse { Succeeded = true, Plans = plans, Abilities = abilities };
    }

    public static ListPlatformPlansResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformPlansResponse { Error = error };
    }
}
