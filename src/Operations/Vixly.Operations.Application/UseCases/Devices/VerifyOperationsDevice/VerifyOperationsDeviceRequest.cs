namespace Vixly.Operations.Application.UseCases.Devices.VerifyOperationsDevice;

// =========================================================
// FILE: VerifyOperationsDeviceRequest.cs
//
// PURPOSE:
// Checks the device of a staff session with Platform (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VerifyOperationsDeviceRequest
{
    public Guid DeviceId { get; init; }

    public string Secret { get; init; } = string.Empty;

    // The company's address, for the link in the administrators' email when the device is blocked (owner, R-6).
    public string? PortalSlug { get; init; }
}
