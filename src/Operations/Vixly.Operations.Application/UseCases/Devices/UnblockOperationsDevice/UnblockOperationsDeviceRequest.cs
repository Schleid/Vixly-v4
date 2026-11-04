namespace Vixly.Operations.Application.UseCases.Devices.UnblockOperationsDevice;

// =========================================================
// FILE: UnblockOperationsDeviceRequest.cs
//
// PURPOSE:
// Lifts the block of a device used from another country, with a fresh authenticator code (owner, R-6).
//
// RESPONSIBILITIES:
// - Carry the device and the administrator's code
//
// IMPORTANT NOTES:
// - Validated by the use case
// =========================================================
public sealed class UnblockOperationsDeviceRequest
{
    public Guid DeviceId { get; init; }

    public string Code { get; init; } = string.Empty;
}
