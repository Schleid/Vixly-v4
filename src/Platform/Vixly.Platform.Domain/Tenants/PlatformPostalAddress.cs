using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformPostalAddress.cs
//
// PURPOSE:
// A postal address of a company.
//
// RESPONSIBILITIES:
// - Validate and hold the address lines, city, region, postal code
//   and country
//
// IMPORTANT NOTES:
// - Immutable; a change is a new instance
// - State or province and postal code are optional: not every
//   country uses them
// - Mapped by EF as part of its owner (no table of its own)
// =========================================================
public sealed class PlatformPostalAddress
{
    private PlatformPostalAddress()
    {
        Line1 = string.Empty;
        City = string.Empty;
        CountryCode = string.Empty;
    }

    public string Line1 { get; private set; }

    public string? Line2 { get; private set; }

    public string City { get; private set; }

    public string? StateProvince { get; private set; }

    public string? PostalCode { get; private set; }

    public string CountryCode { get; private set; }

    public static PlatformPostalAddress Create(
        string? line1,
        string? line2,
        string? city,
        string? stateProvince,
        string? postalCode,
        string? countryCode)
    {
        return new PlatformPostalAddress
        {
            Line1 = PlatformTenantText.Required(
                line1,
                PlatformTenantLimits.AddressLineMaxLength,
                PlatformTenantErrors.AddressRequired,
                PlatformTenantErrors.AddressTooLong),
            Line2 = PlatformTenantText.Optional(line2, PlatformTenantLimits.AddressLineMaxLength, PlatformTenantErrors.AddressTooLong),
            City = PlatformTenantText.Required(
                city,
                PlatformTenantLimits.CityMaxLength,
                PlatformTenantErrors.CityRequired,
                PlatformTenantErrors.CityTooLong),
            StateProvince = PlatformTenantText.Optional(
                stateProvince,
                PlatformTenantLimits.StateProvinceMaxLength,
                PlatformTenantErrors.StateProvinceTooLong),
            PostalCode = PlatformTenantText.Optional(postalCode, PlatformTenantLimits.PostalCodeMaxLength, PlatformTenantErrors.PostalCodeTooLong),
            CountryCode = PlatformCountryCodes.Normalize(countryCode)
        };
    }
}
