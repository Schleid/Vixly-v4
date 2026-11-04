using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.UnblockOperationsDevice;

// =========================================================
// FILE: UnblockOperationsDeviceResponse.cs
//
// PURPOSE:
// Lifts the block of a device used from another country, with a fresh authenticator code (owner, R-6).
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UnblockOperationsDeviceResponse
{
    private UnblockOperationsDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UnblockOperationsDeviceResponse Success()
    {
        return new UnblockOperationsDeviceResponse { Succeeded = true };
    }

    public static UnblockOperationsDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UnblockOperationsDeviceResponse { Error = error };
    }
}
