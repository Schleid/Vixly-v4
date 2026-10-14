namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: StartPlatformRegistrationBody.cs
//
// PURPOSE:
// The public registration form.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Validated by Platform; nothing here is trusted
// - BotToken is the Turnstile token of the form (D-T12)
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record StartPlatformRegistrationBody
{
    public string LegalName { get; init; } = string.Empty;

    public string? TradingName { get; init; }

    public string CompanyEmail { get; init; } = string.Empty;

    public string CompanyPhone { get; init; } = string.Empty;

    public string AddressLine1 { get; init; } = string.Empty;

    public string? AddressLine2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string? StateProvince { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string OwnerName { get; init; } = string.Empty;

    public string OwnerEmail { get; init; } = string.Empty;

    public string OwnerPhone { get; init; } = string.Empty;

    public string Language { get; init; } = "en";

    public string? TimeZone { get; init; }

    public string Slug { get; init; } = string.Empty;

    public bool AcceptedTerms { get; init; }

    public string TermsVersion { get; init; } = string.Empty;

    public string PrivacyVersion { get; init; } = string.Empty;

    public string? BotToken { get; init; }
}
