using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleAccount;

// =========================================================
// FILE: GetPlatformZelleAccountResponse.cs
//
// PURPOSE:
// Reads Vixly's Zelle details and QR image for the payments settings page.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformZelleAccountResponse
{
    private GetPlatformZelleAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformZelleAccountItem? Account { get; private init; }

    public PlatformZelleImage? QrCode { get; private init; }

    public static GetPlatformZelleAccountResponse Success(PlatformZelleAccountItem? account, PlatformZelleImage? qrCode)
    {
        return new GetPlatformZelleAccountResponse { Succeeded = true, Account = account, QrCode = qrCode };
    }

    public static GetPlatformZelleAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformZelleAccountResponse { Error = error };
    }
}
