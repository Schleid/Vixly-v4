namespace Vixly.Operations.Application.UseCases.Devices.DecideOperationsDevice;

// =========================================================
// FILE: DecideOperationsDeviceRequest.cs
//
// PURPOSE:
// Approves or denies a computer waiting to join, with a fresh authenticator code (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecideOperationsDeviceRequest
{
    public Guid DeviceId { get; init; }

    public bool Approve { get; init; }

    public string Code { get; init; } = string.Empty;
}
