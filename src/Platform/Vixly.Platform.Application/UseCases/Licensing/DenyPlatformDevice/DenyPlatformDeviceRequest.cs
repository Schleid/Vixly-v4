using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.DenyPlatformDevice;

// =========================================================
// FILE: DenyPlatformDeviceRequest.cs
//
// PURPOSE:
// A tenant admin refuses a waiting device (D-L4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DenyPlatformDeviceRequest
{
    public Guid DeviceId { get; init; }
}
