namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsPostalAddress.cs
//
// PURPOSE:
// A street address with its own country (Slice 1B).
//
// RESPONSIBILITIES:
// - Validate each part
//
// IMPORTANT NOTES:
// - A location is always listed under the country of its own address (owner, 2026-10-18)
// =========================================================
public sealed class OperationsPostalAddress
{
    private OperationsPostalAddress()
    {
        Line1 = string.Empty;
        City = string.Empty;
        CountryCode = string.Empty;
    }

    public string Line1 { get; private set; }

    public string? Line2 { get; private set; }

    public string City { get; private set; }

    public string? Region { get; private set; }

    public string? PostalCode { get; private set; }

    public string CountryCode { get; private set; }

    public static OperationsPostalAddress Create(string? line1, string? line2, string? city, string? region, string? postalCode, string? countryCode)
    {
        return new OperationsPostalAddress
        {
            Line1 = OperationsLocationText.Required(line1, OperationsLocationLimits.AddressLineMaxLength, OperationsLocationErrors.AddressRequired, OperationsLocationErrors.AddressTooLong),
            Line2 = OperationsLocationText.Optional(line2, OperationsLocationLimits.AddressLineMaxLength, OperationsLocationErrors.AddressTooLong),
            City = OperationsLocationText.Required(city, OperationsLocationLimits.CityMaxLength, OperationsLocationErrors.CityRequired, OperationsLocationErrors.CityTooLong),
            Region = OperationsLocationText.Optional(region, OperationsLocationLimits.RegionMaxLength, OperationsLocationErrors.RegionTooLong),
            PostalCode = OperationsLocationText.Optional(postalCode, OperationsLocationLimits.PostalCodeMaxLength, OperationsLocationErrors.PostalCodeTooLong),
            CountryCode = OperationsCountryCodes.Normalize(countryCode)
        };
    }
}
