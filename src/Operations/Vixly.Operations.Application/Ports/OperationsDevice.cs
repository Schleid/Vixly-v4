namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsDevice.cs
//
// PURPOSE:
// A device of the tenant as Operations shows it.
//
// RESPONSIBILITIES:
// - Carry what the Devices screen lists
//
// IMPORTANT NOTES:
// - From Platform; Operations keeps no device records
// - Browser, ApprovedUtc, ApprovedByName (none and approved: the Owner's email link) for the company's list (owner, 2026-10-21).
// =========================================================
public sealed record OperationsDevice(Guid DeviceId, string Number, string Name, string Kind, string Status, bool SeatHeld, string? RequestedByName, DateTimeOffset RequestedUtc, DateTimeOffset? LastSeenUtc,
    string? Browser = null, DateTimeOffset? ApprovedUtc = null, string? ApprovedByName = null);
