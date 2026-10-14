using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformOneTimeCharge;

// =========================================================
// FILE: CancelPlatformOneTimeChargeResponse.cs
//
// PURPOSE:
// Cancels a one-time charge that is not billed yet (D-S23).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelPlatformOneTimeChargeResponse
{
    private CancelPlatformOneTimeChargeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelPlatformOneTimeChargeResponse Success()
    {
        return new CancelPlatformOneTimeChargeResponse { Succeeded = true };
    }

    public static CancelPlatformOneTimeChargeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelPlatformOneTimeChargeResponse { Error = error };
    }
}
