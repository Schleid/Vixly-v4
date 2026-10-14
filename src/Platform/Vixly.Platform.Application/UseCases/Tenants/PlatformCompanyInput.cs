using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformCompanyInput.cs
//
// PURPOSE:
// The company fields a form sends, and their conversion to
// validated details.
//
// RESPONSIBILITIES:
// - Carry legal and trading name, email, phone and address
// - Build PlatformCompanyDetails, throwing coded errors
//
// IMPORTANT NOTES:
// - Shared by staff and public forms so both validate alike
// =========================================================
public sealed class PlatformCompanyInput
{
    public string LegalName { get; init; } = string.Empty;

    public string? TradingName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string AddressLine1 { get; init; } = string.Empty;

    public string? AddressLine2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string? StateProvince { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public PlatformCompanyDetails ToDetails()
    {
        return PlatformCompanyDetails.Create(
            LegalName,
            TradingName,
            EmailAddress.Create(Email),
            PhoneNumber.Create(Phone),
            PlatformPostalAddress.Create(AddressLine1, AddressLine2, City, StateProvince, PostalCode, CountryCode));
    }
}
