using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Arrivals;

// =========================================================
// FILE: OperationsArrivalErrors.cs
//
// PURPOSE:
// Error codes of destination receiving (Slice 7).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsArrivalErrors
{
    public static Error NotFound { get; } = Error.Create("ops.arrivals.not_found");

    public static Error NotDeparted { get; } = Error.Create("ops.arrivals.not_departed");

    public static Error Received { get; } = Error.Create("ops.arrivals.received");

    public static Error UnknownCode { get; } = Error.Create("ops.arrivals.unknown_code");

    public static Error BundlesUnopened { get; } = Error.Create("ops.arrivals.bundles_unopened");

    public static Error CustomsNotHere { get; } = Error.Create("ops.arrivals.customs_not_here");

    public static Error ReasonRequired { get; } = Error.Create("ops.arrivals.reason_required");

    public static Error NotOpen { get; } = Error.Create("ops.arrivals.not_open");

    public static Error CustomsStepInvalid { get; } = Error.Create("ops.arrivals.customs_step_invalid");

    public static Error NotHere { get; } = Error.Create("ops.arrivals.not_here");

    public static Error MovedOn { get; } = Error.Create("ops.arrivals.moved_on");
}
