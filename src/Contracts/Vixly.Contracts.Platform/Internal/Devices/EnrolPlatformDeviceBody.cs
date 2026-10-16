namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: EnrolPlatformDeviceBody.cs
//
// PURPOSE:
// A device asking for a seat (call 4).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Kind: pc, phone, scanner, tablet, other; a device with a key pair sends its public key (D-L5)
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record EnrolPlatformDeviceBody(string? Name, string Kind, string? PublicKey);
