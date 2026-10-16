using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.RequestPlatformFirstDeviceLink;

// =========================================================
// FILE: RequestPlatformFirstDeviceLinkResponse.cs
//
// PURPOSE:
// After account setup, Operations asks for the owner's first-device link (D-L7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestPlatformFirstDeviceLinkResponse
{
    private RequestPlatformFirstDeviceLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestPlatformFirstDeviceLinkResponse Success()
    {
        return new RequestPlatformFirstDeviceLinkResponse { Succeeded = true };
    }

    public static RequestPlatformFirstDeviceLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestPlatformFirstDeviceLinkResponse { Error = error };
    }
}
