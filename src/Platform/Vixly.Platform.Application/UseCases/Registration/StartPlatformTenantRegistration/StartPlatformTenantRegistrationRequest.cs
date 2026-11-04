using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;

// =========================================================
// FILE: StartPlatformTenantRegistrationRequest.cs
//
// PURPOSE:
// Input for StartPlatformTenantRegistration.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain
// =========================================================
public sealed class StartPlatformTenantRegistrationRequest
{
    public PlatformCompanyInput Company { get; init; } = new();

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

    public string? OfferToken { get; init; }
}
