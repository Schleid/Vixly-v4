namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformSeatsChangedEvent.cs
//
// PURPOSE:
// The seats a tenant uses or may use changed.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Cause: device or subscription; read the device list or entitlements again
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformSeatsChangedEvent(Guid TenantId, string Cause, DateTimeOffset OccurredUtc);
