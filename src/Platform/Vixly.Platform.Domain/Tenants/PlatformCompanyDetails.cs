using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformCompanyDetails.cs
//
// PURPOSE:
// The legal and contact details of a company, as Control invoices
// against them.
//
// RESPONSIBILITIES:
// - Validate and hold legal name, trading name, email, phone and
//   address
//
// IMPORTANT NOTES:
// - Immutable; a change is a new instance
// - Shared by the registration and the tenant profile, so both
//   validate the same way
// - No logo: the brand is Operations data (ADR-0012 Amendment 1)
// =========================================================
public sealed class PlatformCompanyDetails
{
    private PlatformCompanyDetails()
    {
        LegalName = string.Empty;
        Email = null!;
        Phone = null!;
        Address = null!;
    }

    public string LegalName { get; private set; }

    public string? TradingName { get; private set; }

    public EmailAddress Email { get; private set; }

    public PhoneNumber Phone { get; private set; }

    public PlatformPostalAddress Address { get; private set; }

    public string DisplayName => TradingName ?? LegalName;

    public static PlatformCompanyDetails Create(
        string? legalName,
        string? tradingName,
        EmailAddress email,
        PhoneNumber phone,
        PlatformPostalAddress address)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(phone);
        ArgumentNullException.ThrowIfNull(address);

        return new PlatformCompanyDetails
        {
            LegalName = PlatformTenantText.Required(
                legalName,
                PlatformTenantLimits.CompanyNameMaxLength,
                PlatformTenantErrors.LegalNameRequired,
                PlatformTenantErrors.LegalNameTooLong),
            TradingName = PlatformTenantText.Optional(
                tradingName,
                PlatformTenantLimits.CompanyNameMaxLength,
                PlatformTenantErrors.TradingNameTooLong),
            Email = email,
            Phone = phone,
            Address = address
        };
    }
}
