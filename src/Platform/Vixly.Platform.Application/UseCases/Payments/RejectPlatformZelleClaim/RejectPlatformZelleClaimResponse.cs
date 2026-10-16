using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.RejectPlatformZelleClaim;

// =========================================================
// FILE: RejectPlatformZelleClaimResponse.cs
//
// PURPOSE:
// Staff could not find the Zelle money: closes the report and tells the customer why.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RejectPlatformZelleClaimResponse
{
    private RejectPlatformZelleClaimResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RejectPlatformZelleClaimResponse Success()
    {
        return new RejectPlatformZelleClaimResponse { Succeeded = true };
    }

    public static RejectPlatformZelleClaimResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RejectPlatformZelleClaimResponse { Error = error };
    }
}
