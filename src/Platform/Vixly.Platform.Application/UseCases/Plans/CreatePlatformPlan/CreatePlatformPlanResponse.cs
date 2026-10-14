using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.CreatePlatformPlan;

// =========================================================
// FILE: CreatePlatformPlanResponse.cs
//
// PURPOSE:
// Creates a plan with its first draft version (D-S1, D-S2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformPlanResponse
{
    private CreatePlatformPlanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformPlanId { get; private init; }

    public static CreatePlatformPlanResponse Success(Guid platformPlanId)
    {
        return new CreatePlatformPlanResponse { Succeeded = true, PlatformPlanId = platformPlanId };
    }

    public static CreatePlatformPlanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformPlanResponse { Error = error };
    }
}
