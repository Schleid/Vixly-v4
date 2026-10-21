using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Pickup;

// =========================================================
// FILE: OperationsPickupErrors.cs
//
// PURPOSE:
// Error codes of the pickup counter (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsPickupErrors
{
    public static Error NotFound { get; } = Error.Create("ops.pickup.not_found");

    public static Error NothingReady { get; } = Error.Create("ops.pickup.nothing_ready");

    public static Error PayFirst { get; } = Error.Create("ops.pickup.pay_first");

    public static Error BillFirst { get; } = Error.Create("ops.pickup.bill_first");

    public static Error Billed { get; } = Error.Create("ops.pickup.billed");

    public static Error NothingToBill { get; } = Error.Create("ops.pickup.nothing_to_bill");

    public static Error AmountInvalid { get; } = Error.Create("ops.pickup.amount_invalid");

    public static Error CollectorInvalid { get; } = Error.Create("ops.pickup.collector_invalid");

    public static Error IdRequired { get; } = Error.Create("ops.pickup.id_required");

    public static Error SignatureRequired { get; } = Error.Create("ops.pickup.signature_required");

    public static Error PhotoRequired { get; } = Error.Create("ops.pickup.photo_required");

    public static Error CodeWrong { get; } = Error.Create("ops.pickup.code_wrong");

    public static Error CodeNotSent { get; } = Error.Create("ops.pickup.code_not_sent");

    public static Error CodeLocked { get; } = Error.Create("ops.pickup.code_locked");

    public static Error CodeWait { get; } = Error.Create("ops.pickup.code_wait");

    public static Error SettingsInvalid { get; } = Error.Create("ops.pickup.settings_invalid");
}
