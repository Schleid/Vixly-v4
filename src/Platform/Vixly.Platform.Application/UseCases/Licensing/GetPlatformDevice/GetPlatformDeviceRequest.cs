namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformDevice;

// =========================================================
// FILE: GetPlatformDeviceRequest.cs
//
// PURPOSE:
// Reads a device with its history and the actions the reader may take.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformDeviceRequest
{
    public Guid DeviceId { get; init; }
}
