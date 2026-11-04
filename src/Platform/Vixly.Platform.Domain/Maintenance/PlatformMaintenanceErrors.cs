using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceErrors.cs
//
// PURPOSE:
// Error codes for maintenance windows.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.maintenance.*; never reword one that shipped
// =========================================================
public static class PlatformMaintenanceErrors
{
    public static Error TimesInvalid { get; } =
        Error.Create("platform.maintenance.times_invalid");

    public static Error TooLong { get; } =
        Error.Create("platform.maintenance.too_long");

    public static Error SitesRequired { get; } =
        Error.Create("platform.maintenance.sites_required");

    public static Error NoticeInvalid { get; } =
        Error.Create("platform.maintenance.notice_invalid");

    public static Error MessageTooLong { get; } =
        Error.Create("platform.maintenance.message_too_long");

    public static Error AlreadyOver { get; } =
        Error.Create("platform.maintenance.already_over");

    public static Error NotFound { get; } =
        Error.Create("platform.maintenance.window_not_found");
}
