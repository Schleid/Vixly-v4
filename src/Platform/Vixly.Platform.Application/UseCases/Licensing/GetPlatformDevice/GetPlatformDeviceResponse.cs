using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformDevice;

// =========================================================
// FILE: GetPlatformDeviceResponse.cs
//
// PURPOSE:
// Reads a device with its history and the actions the reader may take.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformDeviceResponse
{
    private GetPlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformDeviceDetail? Detail { get; private init; }

    public static GetPlatformDeviceResponse Success(PlatformDeviceDetail? detail)
    {
        return new GetPlatformDeviceResponse { Succeeded = true, Detail = detail };
    }

    public static GetPlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformDeviceResponse { Error = error };
    }
}
