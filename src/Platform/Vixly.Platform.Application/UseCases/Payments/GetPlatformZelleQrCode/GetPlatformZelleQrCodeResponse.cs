using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleQrCode;

// =========================================================
// FILE: GetPlatformZelleQrCodeResponse.cs
//
// PURPOSE:
// Serves the Zelle QR code image to the checkout page while Zelle is on.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformZelleQrCodeResponse
{
    private GetPlatformZelleQrCodeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformZelleImage? QrCode { get; private init; }

    public static GetPlatformZelleQrCodeResponse Success(PlatformZelleImage? qrCode)
    {
        return new GetPlatformZelleQrCodeResponse { Succeeded = true, QrCode = qrCode };
    }

    public static GetPlatformZelleQrCodeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformZelleQrCodeResponse { Error = error };
    }
}
