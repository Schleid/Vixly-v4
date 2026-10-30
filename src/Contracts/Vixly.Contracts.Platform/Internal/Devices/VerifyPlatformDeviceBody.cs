namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: VerifyPlatformDeviceBody.cs
//
// PURPOSE:
// A device proving who it is (call 2).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// - Rotate false: a check that never rotates the secret (a sign-in; review 2026-10-21). Country: where the person is, kept as the device's own when it has none yet.
// =========================================================
public sealed record VerifyPlatformDeviceBody(string? Secret, string? Challenge, string? Signature, bool? Rotate = null, string? Country = null);
