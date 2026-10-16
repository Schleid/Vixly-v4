using System.Globalization;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalErrors.cs
//
// PURPOSE:
// Error codes for legal documents and acceptances (Slice 8).
//
// RESPONSIBILITIES:
// - One error per broken rule
//
// IMPORTANT NOTES:
// - Codes only (L-1)
// =========================================================
public static class PlatformLegalErrors
{
    public static Error DocumentNotFound { get; } = Error.Create("platform.legal.document_not_found");

    public static Error NotDraft { get; } = Error.Create("platform.legal.not_draft");

    public static Error VersionInvalid { get; } = Error.Create("platform.legal.version_invalid");

    public static Error VersionTaken { get; } = Error.Create("platform.legal.version_taken");

    public static Error KindInvalid { get; } = Error.Create("platform.legal.kind_invalid");

    public static Error FileInvalid { get; } = Error.Create("platform.legal.file_invalid", ("max", PlatformLegalLimits.MaxFileMegabytes.ToString(CultureInfo.InvariantCulture)));

    public static Error SummaryInvalid { get; } = Error.Create("platform.legal.summary_invalid");

    public static Error LanguagesMissing { get; } = Error.Create("platform.legal.languages_missing");

    public static Error PartsInvalid { get; } = Error.Create("platform.legal.parts_invalid");

    public static Error EffectiveDateInvalid { get; } = Error.Create("platform.legal.effective_date_invalid", ("days", PlatformLegalLimits.NoticeDays.ToString(CultureInfo.InvariantCulture)));

    public static Error NothingToAccept { get; } = Error.Create("platform.legal.nothing_to_accept");

    public static Error VersionChanged { get; } = Error.Create("platform.legal.version_changed");

    public static Error OwnerOnly { get; } = Error.Create("platform.legal.owner_only");

    public static Error AlreadyAccepted { get; } = Error.Create("platform.legal.already_accepted");

    public static Error EvidenceInvalid { get; } = Error.Create("platform.legal.evidence_invalid", ("max", PlatformLegalLimits.MaxEvidenceMegabytes.ToString(CultureInfo.InvariantCulture)));

    public static Error ReasonRequired { get; } = Error.Create("platform.legal.reason_required");

    public static Error ReminderTooSoon { get; } = Error.Create("platform.legal.reminder_too_soon");

    public static Error ActorRequired { get; } = Error.Create("platform.legal.actor_required");

    public static Error TenantNotAsked { get; } = Error.Create("platform.legal.tenant_not_asked");
}
