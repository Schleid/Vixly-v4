namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformDeviceChangedEvent.cs
//
// PURPOSE:
// A device took a seat, was released or was revoked.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Drop the cached device check for the device (D-L5)
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformDeviceChangedEvent(Guid TenantId, Guid DeviceId, string Number, string Status, string? Reason, DateTimeOffset OccurredUtc);
