using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.CompleteOperationsDeviceLink;

// =========================================================
// FILE: CompleteOperationsDeviceLinkResponse.cs
//
// PURPOSE:
// Completes an emailed device link (first device or owner recovery) on the computer that opened it (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompleteOperationsDeviceLinkResponse
{
    private CompleteOperationsDeviceLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DeviceId { get; private init; }

    public string Secret { get; private init; } = string.Empty;

    public static CompleteOperationsDeviceLinkResponse Success(Guid deviceId, string secret)
    {
        return new CompleteOperationsDeviceLinkResponse { Succeeded = true, DeviceId = deviceId, Secret = secret };
    }

    public static CompleteOperationsDeviceLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompleteOperationsDeviceLinkResponse { Error = error };
    }
}
