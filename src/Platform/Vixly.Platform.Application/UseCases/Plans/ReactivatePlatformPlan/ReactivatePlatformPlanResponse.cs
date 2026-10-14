using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.ReactivatePlatformPlan;

// =========================================================
// FILE: ReactivatePlatformPlanResponse.cs
//
// PURPOSE:
// Opens a retired plan to new subscriptions again.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReactivatePlatformPlanResponse
{
    private ReactivatePlatformPlanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReactivatePlatformPlanResponse Success()
    {
        return new ReactivatePlatformPlanResponse { Succeeded = true };
    }

    public static ReactivatePlatformPlanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReactivatePlatformPlanResponse { Error = error };
    }
}
