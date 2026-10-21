using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.VerifyOperationsDevice;

// =========================================================
// FILE: VerifyOperationsDeviceResponse.cs
//
// PURPOSE:
// Checks the device of a staff session with Platform (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VerifyOperationsDeviceResponse
{
    private VerifyOperationsDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Allowed { get; private init; }

    public string? Code { get; private init; }

    public string Access { get; private init; } = string.Empty;

    public string? NewSecret { get; private init; }

    public static VerifyOperationsDeviceResponse Success(bool allowed, string? code, string access, string? newSecret)
    {
        return new VerifyOperationsDeviceResponse { Succeeded = true, Allowed = allowed, Code = code, Access = access, NewSecret = newSecret };
    }

    public static VerifyOperationsDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VerifyOperationsDeviceResponse { Error = error };
    }
}
