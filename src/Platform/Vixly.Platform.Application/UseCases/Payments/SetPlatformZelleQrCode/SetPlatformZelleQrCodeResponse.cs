using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.SetPlatformZelleQrCode;

// =========================================================
// FILE: SetPlatformZelleQrCodeResponse.cs
//
// PURPOSE:
// Replaces or removes the Zelle QR code image shown at checkout.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformZelleQrCodeResponse
{
    private SetPlatformZelleQrCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformZelleQrCodeResponse Success()
    {
        return new SetPlatformZelleQrCodeResponse { Succeeded = true };
    }

    public static SetPlatformZelleQrCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformZelleQrCodeResponse { Error = error };
    }
}
