namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformPostalAddressInfo.cs
//
// PURPOSE:
// A postal address.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPostalAddressInfo(string Line1, string? Line2, string City, string? StateProvince, string? PostalCode, string CountryCode);
