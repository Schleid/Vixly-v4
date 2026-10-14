using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.RejectPlatformPlanVersion;

// =========================================================
// FILE: RejectPlatformPlanVersionResponse.cs
//
// PURPOSE:
// Sends a submitted plan version back to sales with a reason (D-S26).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RejectPlatformPlanVersionResponse
{
    private RejectPlatformPlanVersionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RejectPlatformPlanVersionResponse Success()
    {
        return new RejectPlatformPlanVersionResponse { Succeeded = true };
    }

    public static RejectPlatformPlanVersionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RejectPlatformPlanVersionResponse { Error = error };
    }
}
