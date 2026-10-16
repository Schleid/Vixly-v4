using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.RevokePlatformDevice;

// =========================================================
// FILE: RevokePlatformDeviceResponse.cs
//
// PURPOSE:
// Vixly staff revoke a device for security; its seat is freed (D-L13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RevokePlatformDeviceResponse
{
    private RevokePlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RevokePlatformDeviceResponse Success()
    {
        return new RevokePlatformDeviceResponse { Succeeded = true };
    }

    public static RevokePlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RevokePlatformDeviceResponse { Error = error };
    }
}
