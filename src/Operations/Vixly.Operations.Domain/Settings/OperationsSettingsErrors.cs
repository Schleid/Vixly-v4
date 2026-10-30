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
}
