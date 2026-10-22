using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.DecideOperationsDevice;

// =========================================================
// FILE: DecideOperationsDeviceResponse.cs
//
// PURPOSE:
// Approves or denies a computer waiting to join, with a fresh authenticator code (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecideOperationsDeviceResponse
{
    private DecideOperationsDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecideOperationsDeviceResponse Success()
    {
        return new DecideOperationsDeviceResponse { Succeeded = true };
    }

    public static DecideOperationsDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecideOperationsDeviceResponse { Error = error };
    }
}
