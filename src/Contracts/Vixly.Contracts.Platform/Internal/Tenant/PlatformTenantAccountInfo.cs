namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformTenantAccountInfo.cs
//
// PURPOSE:
// The tenant's own account (calls 15, 17, 18).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformTenantAccountInfo(
    Guid TenantId,
    string TenantCode,
    string Slug,
    string LegalName,
    string? TradingName,
    string Email,
    string Phone,
    PlatformPostalAddressInfo Address,
    string DefaultLanguage,
    string TimeZone,
    string Status,
    IReadOnlyList<PlatformTenantContactInfo> Contacts,
    PlatformSupportInfo? Support);
