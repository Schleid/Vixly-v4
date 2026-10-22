namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsDeviceOrigin.cs
//
// PURPOSE:
// Where the person registering a device is: address, browser and
// country, as this server sees them (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the values to Platform with the registration
//
// IMPORTANT NOTES:
// - Platform keeps them; the company's list shows the browser only
// =========================================================
public sealed record OperationsDeviceOrigin(string? Ip, string? UserAgent, string? Country);
