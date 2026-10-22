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
// - Name, Number, Country: the device as registered, when allowed (owner, 2026-10-21: activity and the country check at sign-in).
// =========================================================
public sealed record OperationsDeviceCheck(bool Allowed, string? Code, string Access, string? NewSecret, string? Name = null, string? Number = null, string? Country = null);
