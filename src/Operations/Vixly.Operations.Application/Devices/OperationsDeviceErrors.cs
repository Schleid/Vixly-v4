using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Devices;

// =========================================================
// FILE: OperationsDeviceErrors.cs
//
// PURPOSE:
// Error codes of devices in Operations (Slice 1C).
//
// RESPONSIBILITIES:
// - Name every refusal of a device action, including those Platform gives
//
// IMPORTANT NOTES:
// - Codes only; texts are in OperationsText
// =========================================================
public static class OperationsDeviceErrors
{
    public static Error LinkInvalid { get; } = Error.Create("ops.devices.link_invalid");

    public static Error NotReady { get; } = Error.Create("ops.devices.not_ready");

    public static Error TooManyLinks { get; } = Error.Create("ops.devices.too_many_links");

    public static Error NoFreeSeat { get; } = Error.Create("ops.devices.no_free_seat");

    public static Error NotAllowed { get; } = Error.Create("ops.devices.not_allowed");

    public static Error CodeRequired { get; } = Error.Create("ops.devices.code_required");

    public static Error NameRequired { get; } = Error.Create("ops.devices.name_required");

    public static Error KindInvalid { get; } = Error.Create("ops.devices.kind_invalid");

    public static Error TooManyPending { get; } = Error.Create("ops.devices.too_many_pending");

    public static Error Refused { get; } = Error.Create("ops.devices.refused");

    // Every type Platform knows (devices registered before 2026-10-21 keep theirs).
    public static IReadOnlyList<string> Kinds { get; } = new[] { "pc", "phone", "tablet", "scanner", "other", "desktop", "laptop" };

    // Owner, 2026-10-21: a new device is a desktop, a laptop or a mobile (phone); the type may serve security later.
    public static IReadOnlyList<string> Offered { get; } = new[] { "desktop", "laptop", "phone" };

    public const int NameMaxLength = 60;
}
