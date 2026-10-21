namespace Vixly.Operations.Application.UseCases.Devices.ReleaseOperationsDevice;

// =========================================================
// FILE: ReleaseOperationsDeviceRequest.cs
//
// PURPOSE:
// Stops a device (lost, broken, staff left, ...) with a fresh authenticator code (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReleaseOperationsDeviceRequest
{
    public Guid DeviceId { get; init; }

    public string Reason { get; init; } = "other";

    public string Code { get; init; } = string.Empty;
}
