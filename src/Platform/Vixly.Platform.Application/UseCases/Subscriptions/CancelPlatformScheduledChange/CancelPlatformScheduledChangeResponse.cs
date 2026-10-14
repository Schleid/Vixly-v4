using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformScheduledChange;

// =========================================================
// FILE: CancelPlatformScheduledChangeResponse.cs
//
// PURPOSE:
// Cancels the change waiting for the period end.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelPlatformScheduledChangeResponse
{
    private CancelPlatformScheduledChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelPlatformScheduledChangeResponse Success()
    {
        return new CancelPlatformScheduledChangeResponse { Succeeded = true };
    }

    public static CancelPlatformScheduledChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelPlatformScheduledChangeResponse { Error = error };
    }
}
