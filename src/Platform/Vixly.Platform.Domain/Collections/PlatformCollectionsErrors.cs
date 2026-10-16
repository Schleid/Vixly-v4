using System.Globalization;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionsErrors.cs
//
// PURPOSE:
// Error codes for collections: settings, cases, notes and extensions.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.collections.*; never reword one that shipped
// =========================================================
public static class PlatformCollectionsErrors
{
    public static Error SettingsInvalid { get; } = Error.Create("platform.collections.settings_invalid");

    public static Error ActorRequired { get; } = Error.Create("platform.collections.actor_required");

    public static Error CaseNotFound { get; } = Error.Create("platform.collections.case_not_found");

    public static Error CaseClosed { get; } = Error.Create("platform.collections.case_closed");

    public static Error ReasonRequired { get; } = Error.Create("platform.collections.reason_required");

    public static Error NoteInvalid { get; } = Error.Create("platform.collections.note_invalid");

    public static Error ExtensionDateInvalid { get; } = Error.Create("platform.collections.extension_date_invalid");

    public static Error ExtensionTooLong(int days) => Error.Create("platform.collections.extension_too_long", ("days", days.ToString(CultureInfo.InvariantCulture)));

    public static Error ExtensionLimit(int max) => Error.Create("platform.collections.extension_limit", ("max", max.ToString(CultureInfo.InvariantCulture)));

    public static IReadOnlyList<string> Codes { get; } = new[]
    {
        "platform.collections.settings_invalid", "platform.collections.actor_required", "platform.collections.case_not_found",
        "platform.collections.case_closed", "platform.collections.reason_required", "platform.collections.note_invalid",
        "platform.collections.extension_date_invalid", "platform.collections.extension_too_long", "platform.collections.extension_limit"
    };
}
