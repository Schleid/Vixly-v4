using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.GetPlatformPlan;

// =========================================================
// FILE: GetPlatformPlanResponse.cs
//
// PURPOSE:
// Reads one plan for the plan page.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformPlanResponse
{
    private GetPlatformPlanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformPlanDetail? Plan { get; private init; }

    public PlatformPlanAbilities Abilities { get; private init; } = PlatformPlanAbilities.None;

    public static GetPlatformPlanResponse Success(PlatformPlanDetail? plan, PlatformPlanAbilities abilities)
    {
        return new GetPlatformPlanResponse { Succeeded = true, Plan = plan, Abilities = abilities };
    }

    public static GetPlatformPlanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformPlanResponse { Error = error };
    }
}
