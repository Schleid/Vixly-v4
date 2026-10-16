namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: CompletePlatformDeviceLinkBody.cs
//
// PURPOSE:
// A device enrolled from an emailed link (call 11).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record CompletePlatformDeviceLinkBody(string Token, string? Name, string Kind, string? PublicKey, Guid? ReplaceDeviceId);
