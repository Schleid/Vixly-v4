namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsDeviceSeats.cs
//
// PURPOSE:
// The company's licences as Platform counts them: bought, in use,
// and a lower number scheduled for the renewal (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the numbers to the Licences tab
//
// IMPORTANT NOTES:
// - A decrease waits for the renewal; the devices in use must be released first
// =========================================================
public sealed record OperationsDeviceSeats(int Seats, int InUse, int? ScheduledSeats, DateTimeOffset? ScheduledUtc);
