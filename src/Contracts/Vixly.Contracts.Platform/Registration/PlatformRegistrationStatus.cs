namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformRegistrationStatus.cs
//
// PURPOSE:
// Where a registration stands and what comes next.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Step codes: pending_email_verification, pending_plan_selection,
//   and later steps as their slices open
// - Never carries the stored company or owner details
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformRegistrationStatus(
    string Step,
    string MaskedEmail,
    string Slug,
    DateTimeOffset VerificationDeadlineUtc);
