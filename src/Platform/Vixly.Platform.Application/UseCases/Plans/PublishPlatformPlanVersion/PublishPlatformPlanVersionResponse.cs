using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.PublishPlatformPlanVersion;

// =========================================================
// FILE: PublishPlatformPlanVersionResponse.cs
//
// PURPOSE:
// Publishes the draft version of a plan (D-S2, D-S13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PublishPlatformPlanVersionResponse
{
    private PublishPlatformPlanVersionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static PublishPlatformPlanVersionResponse Success()
    {
        return new PublishPlatformPlanVersionResponse { Succeeded = true };
    }

    public static PublishPlatformPlanVersionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PublishPlatformPlanVersionResponse { Error = error };
    }
}
