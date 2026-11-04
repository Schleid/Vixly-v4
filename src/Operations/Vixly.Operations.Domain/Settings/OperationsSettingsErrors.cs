using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsSettingsErrors.cs
//
// PURPOSE:
// Error codes of the tenant settings (Slice 1E).
//
// RESPONSIBILITIES:
// - Name every refusal of a settings change
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsSettingsErrors
{
    public static Error CurrencyInvalid { get; } = Error.Create("ops.settings.currency_invalid");

    public static Error CurrencyLocked { get; } = Error.Create("ops.settings.currency_locked");

    public static Error UnitInvalid { get; } = Error.Create("ops.settings.unit_invalid");

    public static Error DeliveryModeRequired { get; } = Error.Create("ops.settings.delivery_mode_required");

    public static Error PrefixInvalid { get; } = Error.Create("ops.settings.prefix_invalid", ("maxLength", OperationsSettingsLimits.PrefixMaxLength));

    public static Error PrefixesMustDiffer { get; } = Error.Create("ops.settings.prefixes_must_differ");

    // Owner, 2026-10-28: two letters A to Z.
    public static Error TrackingPrefixInvalid { get; } = Error.Create("ops.settings.tracking_prefix_invalid");

    public static Error StaffIdleInvalid { get; } = Error.Create("ops.settings.staff_idle_invalid",
        ("min", OperationsSettingsLimits.StaffIdleMinutesMin), ("max", OperationsSettingsLimits.StaffIdleMinutesMax));

    public static Error CustomerIdleInvalid { get; } = Error.Create("ops.settings.customer_idle_invalid",
        ("min", OperationsSettingsLimits.CustomerIdleMinutesMin), ("max", OperationsSettingsLimits.CustomerIdleMinutesMax));

    public static Error IdleWarnInvalid { get; } = Error.Create("ops.settings.idle_warn_invalid",
        ("min", OperationsSettingsLimits.IdleWarnMinutesMin), ("max", OperationsSettingsLimits.IdleWarnMinutesMax));
}
