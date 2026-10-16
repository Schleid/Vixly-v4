namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: PlatformDeviceCheckInfo.cs
//
// PURPOSE:
// Whether a device may work now (call 2).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Cache a positive answer at most 5 minutes; never a refusal (D-K6)
// - NewSecret: replace the stored secret at once
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformDeviceCheckInfo(bool Allowed, string? Code, string Access, string? NewSecret);
