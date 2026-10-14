using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.SubmitPlatformPlanVersion;

// =========================================================
// FILE: SubmitPlatformPlanVersionResponse.cs
//
// PURPOSE:
// Submits a plan's draft version for CFO approval (D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SubmitPlatformPlanVersionResponse
{
    private SubmitPlatformPlanVersionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SubmitPlatformPlanVersionResponse Success()
    {
        return new SubmitPlatformPlanVersionResponse { Succeeded = true };
    }

    public static SubmitPlatformPlanVersionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SubmitPlatformPlanVersionResponse { Error = error };
    }
}
