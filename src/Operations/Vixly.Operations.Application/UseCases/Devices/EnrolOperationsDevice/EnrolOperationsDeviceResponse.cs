using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.EnrolOperationsDevice;

// =========================================================
// FILE: EnrolOperationsDeviceResponse.cs
//
// PURPOSE:
// Asks to add the computer a signed-in staff member is using; an Owner approves it (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EnrolOperationsDeviceResponse
{
    private EnrolOperationsDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DeviceId { get; private init; }

    public string Secret { get; private init; } = string.Empty;

    public string Status { get; private init; } = string.Empty;

    public static EnrolOperationsDeviceResponse Success(Guid deviceId, string secret, string status)
    {
        return new EnrolOperationsDeviceResponse { Succeeded = true, DeviceId = deviceId, Secret = secret, Status = status };
    }

    public static EnrolOperationsDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EnrolOperationsDeviceResponse { Error = error };
    }
}
