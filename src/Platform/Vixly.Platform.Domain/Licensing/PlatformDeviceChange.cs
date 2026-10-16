namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceChange.cs
//
// PURPOSE:
// The result of a device change: its history line and what happened to its seat.
//
// RESPONSIBILITIES:
// - Tell the caller whether a seat was taken or freed, in the same commit
//
// IMPORTANT NOTES:
// - The seat count lives on PlatformTenantLicensing
// =========================================================
public sealed record PlatformDeviceChange(PlatformDeviceEvent Event, bool SeatTaken, bool SeatFreed);
