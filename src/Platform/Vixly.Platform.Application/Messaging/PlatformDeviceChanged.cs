using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformDeviceChanged.cs
//
// PURPOSE:
// Outbox message for Operations: a device was approved, released or revoked (D-L14).
//
// RESPONSIBILITIES:
// - Carry the tenant, device, number, new state and reason
//
// IMPORTANT NOTES:
// - Operations drops cached checks for the device
// =========================================================
public sealed record PlatformDeviceChanged(
    Guid PlatformTenantId,
    Guid DeviceId,
    string Number,
    PlatformDeviceStatus Status,
    PlatformDeviceReason? Reason,
    DateTimeOffset OccurredUtc);
