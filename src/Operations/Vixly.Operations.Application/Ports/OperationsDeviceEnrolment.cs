namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsDeviceEnrolment.cs
//
// PURPOSE:
// A device Platform has just enrolled.
//
// RESPONSIBILITIES:
// - Carry the id, number, status and the first secret
//
// IMPORTANT NOTES:
// - The secret is shown to nobody: it goes straight into the device cookie
// =========================================================
public sealed record OperationsDeviceEnrolment(Guid DeviceId, string Number, string Status, string? Secret);
