using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;

// =========================================================
// FILE: ApprovePlatformDeviceRequest.cs
//
// PURPOSE:
// A tenant admin approves a waiting device; it takes a free seat (D-L2, D-L4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ApprovePlatformDeviceRequest
{
    public Guid DeviceId { get; init; }
}
