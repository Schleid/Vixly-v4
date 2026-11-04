namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: PlatformDeviceSeatsInfo.cs
//
// PURPOSE:
// The company's licences: bought, in use, and a lower number scheduled for the renewal (owner, 2026-10-21: the Licences tab).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// - A decrease waits for the renewal and needs the devices in use released first (as V3)
// =========================================================
public sealed record PlatformDeviceSeatsInfo(int Seats, int InUse, int? ScheduledSeats, DateTimeOffset? ScheduledUtc);
