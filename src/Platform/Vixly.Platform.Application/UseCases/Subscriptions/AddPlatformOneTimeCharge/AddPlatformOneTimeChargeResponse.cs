using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.AddPlatformOneTimeCharge;

// =========================================================
// FILE: AddPlatformOneTimeChargeResponse.cs
//
// PURPOSE:
// Adds a one-time charge, billed on the next invoice (D-S23).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddPlatformOneTimeChargeResponse
{
    private AddPlatformOneTimeChargeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AddPlatformOneTimeChargeResponse Success()
    {
        return new AddPlatformOneTimeChargeResponse { Succeeded = true };
    }

    public static AddPlatformOneTimeChargeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddPlatformOneTimeChargeResponse { Error = error };
    }
}
