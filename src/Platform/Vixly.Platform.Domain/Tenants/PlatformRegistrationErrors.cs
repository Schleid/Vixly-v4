using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformRegistrationErrors.cs
//
// PURPOSE:
// The errors raised by self-service registration.
//
// RESPONSIBILITIES:
// - Declare every error code of this area in one place
//
// IMPORTANT NOTES:
// - Codes start with platform.registrations. (Global Standard 4.2)
// - Each code needs en, fr and es text (L-10)
// - Never reword a code that has shipped. Add a new one
// =========================================================
public static class PlatformRegistrationErrors
{
    public static Error OwnerNameRequired { get; } =
        Error.Create("platform.registrations.owner_name_required");

    public static Error OwnerNameTooLong { get; } =
        Error.Create("platform.registrations.owner_name_too_long", ("maxLength", PlatformTenantLimits.ContactNameMaxLength));

    public static Error TermsNotAccepted { get; } =
        Error.Create("platform.registrations.terms_not_accepted");

    public static Error LegalVersionInvalid { get; } =
        Error.Create("platform.registrations.legal_version_invalid");

    public static Error LinkInvalid { get; } =
        Error.Create("platform.registrations.link_invalid");

    public static Error AlreadyVerified { get; } =
        Error.Create("platform.registrations.already_verified");

    public static Error NotVerified { get; } =
        Error.Create("platform.registrations.not_verified");

    public static Error Expired { get; } =
        Error.Create("platform.registrations.expired");

    public static Error Cancelled { get; } =
        Error.Create("platform.registrations.cancelled");

    public static Error Closed { get; } =
        Error.Create("platform.registrations.closed");

    public static Error ResendTooSoon { get; } =
        Error.Create("platform.registrations.resend_too_soon", ("maxCount", PlatformTenantLimits.MaxVerificationSendsPerHour));

    public static Error EmailUnchanged { get; } =
        Error.Create("platform.registrations.email_unchanged");

    public static Error NotFound { get; } =
        Error.Create("platform.registrations.not_found");

    public static Error AccessHashInvalid { get; } =
        Error.Create("platform.registrations.access_hash_invalid");
}
