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
// =========================================================
public sealed record OperationsDevice(Guid DeviceId, string Number, string Name, string Kind, string Status, bool SeatHeld, string? RequestedByName, DateTimeOffset RequestedUtc, DateTimeOffset? LastSeenUtc);
