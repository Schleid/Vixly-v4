using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.SendPlatformFirstDeviceLink;

// =========================================================
// FILE: SendPlatformFirstDeviceLinkResponse.cs
//
// PURPOSE:
// Vixly staff email the owner a first-device link (tenants created by staff, D-L7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendPlatformFirstDeviceLinkResponse
{
    private SendPlatformFirstDeviceLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendPlatformFirstDeviceLinkResponse Success()
    {
        return new SendPlatformFirstDeviceLinkResponse { Succeeded = true };
    }

    public static SendPlatformFirstDeviceLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendPlatformFirstDeviceLinkResponse { Error = error };
    }
}
