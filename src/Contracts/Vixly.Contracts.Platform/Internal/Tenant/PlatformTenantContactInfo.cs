namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformTenantContactInfo.cs
//
// PURPOSE:
// One tenant contact.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Role: owner, billing, technical
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformTenantContactInfo(string Role, string Name, string Email, string? Phone);
