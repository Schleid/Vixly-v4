using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformCompanyProfileItem.cs
//
// PURPOSE:
// The company profile (issuer details on documents), as shown and edited.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformCompanyProfileItem(
    string LegalName,
    string? TradingName,
    string Email,
    string Phone,
    string Line1,
    string? Line2,
    string City,
    string? StateProvince,
    string? PostalCode,
    string CountryCode,
    string? TaxId,
    string? Website,
    bool HasLogo,
    string? SupportEmail = null,
    string? SupportPhone = null);
