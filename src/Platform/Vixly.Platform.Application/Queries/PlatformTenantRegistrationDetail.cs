using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantRegistrationDetail.cs
//
// PURPOSE:
// Everything the registration page shows.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - The access token and link hashes are never exposed
// =========================================================
public sealed record PlatformTenantRegistrationDetail(
    Guid RegistrationId,
    PlatformTenantRegistrationStatus Status,
    string LegalName,
    string? TradingName,
    string CompanyEmail,
    string CompanyPhone,
    string CountryCode,
    string OwnerName,
    string OwnerEmail,
    string OwnerPhone,
    string Language,
    string TimeZone,
    string Slug,
    string TermsVersion,
    string PrivacyVersion,
    string? ConsentIpAddress,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? LastVerificationSentUtc,
    DateTimeOffset? EmailVerifiedUtc,
    DateTimeOffset? ClosedUtc,
    string? CancelledByName,
    string? CancellationReason,
    Guid? PlatformTenantId);
