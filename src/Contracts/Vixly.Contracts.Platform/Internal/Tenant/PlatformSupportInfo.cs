namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformSupportInfo.cs
//
// PURPOSE:
// How a tenant reaches Vixly support.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformSupportInfo(string CompanyName, string? Email, string? Phone, string? Website);
