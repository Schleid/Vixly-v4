namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: UpdatePlatformTenantAccountBody.cs
//
// PURPOSE:
// What the tenant changes on its account (call 16).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Null keeps a value; a contacts list replaces the billing and technical contacts
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record UpdatePlatformTenantAccountBody(IReadOnlyList<PlatformTenantContactBody>? Contacts, string? Language, string? TimeZone);
