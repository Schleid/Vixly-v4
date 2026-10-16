namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformTenantContactBody.cs
//
// PURPOSE:
// A billing or technical contact the tenant sets.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Role: billing or technical; the owner contact is changed by Vixly staff
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformTenantContactBody(string Role, string Name, string Email, string? Phone);
