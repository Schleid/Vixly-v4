using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.RetirePlatformPlan;

// =========================================================
// FILE: RetirePlatformPlanResponse.cs
//
// PURPOSE:
// Retires a plan: no new subscriptions; subscribers stay on it.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RetirePlatformPlanResponse
{
    private RetirePlatformPlanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RetirePlatformPlanResponse Success()
    {
        return new RetirePlatformPlanResponse { Succeeded = true };
    }

    public static RetirePlatformPlanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RetirePlatformPlanResponse { Error = error };
    }
}
