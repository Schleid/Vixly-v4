namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: PlatformDeviceReleaseInfo.cs
//
// PURPOSE:
// What happened to a release.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - AwaitsReview: over the allowance; Vixly staff decide (D-L6)
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformDeviceReleaseInfo(string Status, bool AwaitsReview);
