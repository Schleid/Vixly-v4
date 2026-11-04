using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.ReleaseOperationsDevice;

// =========================================================
// FILE: ReleaseOperationsDeviceResponse.cs
//
// PURPOSE:
// Stops a device (lost, broken, staff left, ...) with a fresh authenticator code (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReleaseOperationsDeviceResponse
{
    private ReleaseOperationsDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReleaseOperationsDeviceResponse Success()
    {
        return new ReleaseOperationsDeviceResponse { Succeeded = true };
    }

    public static ReleaseOperationsDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReleaseOperationsDeviceResponse { Error = error };
    }
}
