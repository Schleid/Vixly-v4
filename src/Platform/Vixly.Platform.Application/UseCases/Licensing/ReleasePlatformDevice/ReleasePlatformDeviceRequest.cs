using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.ReleasePlatformDevice;

// =========================================================
// FILE: ReleasePlatformDeviceRequest.cs
//
// PURPOSE:
// A tenant admin frees a device's seat: at once within the allowance, otherwise Vixly reviews it; a lost device stops at once (D-L6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReleasePlatformDeviceRequest
{
    public Guid DeviceId { get; init; }

    public PlatformDeviceReason Reason { get; init; }
}
