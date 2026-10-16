namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: ReleasePlatformDeviceBody.cs
//
// PURPOSE:
// Why a device gives back its seat (call 7).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record ReleasePlatformDeviceBody(string Reason);
