namespace Vixly.Platform.Application.UseCases.Licensing.RevokePlatformDevice;

// =========================================================
// FILE: RevokePlatformDeviceRequest.cs
//
// PURPOSE:
// Vixly staff revoke a device for security; its seat is freed (D-L13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RevokePlatformDeviceRequest
{
    public Guid DeviceId { get; init; }

    public string? Note { get; init; }
}
