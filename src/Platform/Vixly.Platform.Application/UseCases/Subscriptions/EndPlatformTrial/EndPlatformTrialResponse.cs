using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformTrial;

// =========================================================
// FILE: EndPlatformTrialResponse.cs
//
// PURPOSE:
// Ends a trial now: the subscription then awaits payment (owner, D-S12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndPlatformTrialResponse
{
    private EndPlatformTrialResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static EndPlatformTrialResponse Success()
    {
        return new EndPlatformTrialResponse { Succeeded = true };
    }

    public static EndPlatformTrialResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndPlatformTrialResponse { Error = error };
    }
}
