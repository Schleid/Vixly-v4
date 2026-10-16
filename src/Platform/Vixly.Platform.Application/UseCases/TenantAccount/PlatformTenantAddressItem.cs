namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantAddressItem.cs
//
// PURPOSE:
// The tenant's postal address.
//
// RESPONSIBILITIES:
// - Carry what Operations shows the tenant
//
// IMPORTANT NOTES:
// - Plain values for the wire mapping (D-K10)
// =========================================================
public sealed record PlatformTenantAddressItem(string Line1, string? Line2, string City, string? StateProvince, string? PostalCode, string CountryCode);
