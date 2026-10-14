using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ExtendPlatformTrial;

// =========================================================
// FILE: ExtendPlatformTrialResponse.cs
//
// PURPOSE:
// Moves a trial's end date (owner, D-S12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ExtendPlatformTrialResponse
{
    private ExtendPlatformTrialResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ExtendPlatformTrialResponse Success()
    {
        return new ExtendPlatformTrialResponse { Succeeded = true };
    }

    public static ExtendPlatformTrialResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ExtendPlatformTrialResponse { Error = error };
    }
}
