namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsDeviceCheck.cs
//
// PURPOSE:
// Platform's answer to a device check.
//
// RESPONSIBILITIES:
// - Carry allowed or the refusal code, the access level and a new secret when rotating
//
// IMPORTANT NOTES:
// - Codes are Platform wire codes (device_pending, device_revoked, ...)
// =========================================================
public sealed record OperationsDeviceCheck(bool Allowed, string? Code, string Access, string? NewSecret);
