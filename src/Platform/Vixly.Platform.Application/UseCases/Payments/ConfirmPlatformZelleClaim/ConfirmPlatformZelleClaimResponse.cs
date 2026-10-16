using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ConfirmPlatformZelleClaim;

// =========================================================
// FILE: ConfirmPlatformZelleClaimResponse.cs
//
// PURPOSE:
// Staff found the Zelle money: records a Zelle payment on the invoice and closes the report.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ConfirmPlatformZelleClaimResponse
{
    private ConfirmPlatformZelleClaimResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ConfirmPlatformZelleClaimResponse Success()
    {
        return new ConfirmPlatformZelleClaimResponse { Succeeded = true };
    }

    public static ConfirmPlatformZelleClaimResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConfirmPlatformZelleClaimResponse { Error = error };
    }
}
