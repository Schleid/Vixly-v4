using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchErrors.cs
//
// PURPOSE:
// Error codes of carriers and dispatches (Slice 6B).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsDispatchErrors
{
    public static Error NotFound { get; } = Error.Create("ops.dispatch.not_found");

    public static Error CarrierInvalid { get; } = Error.Create("ops.dispatch.carrier_invalid");

    public static Error CarrierTaken { get; } = Error.Create("ops.dispatch.carrier_taken");

    public static Error CarrierInactive { get; } = Error.Create("ops.dispatch.carrier_inactive");

    public static Error RateStarted { get; } = Error.Create("ops.dispatch.rate_started");

    public static Error PrefixInvalid { get; } = Error.Create("ops.dispatch.prefix_invalid");

    public static Error RateInvalid { get; } = Error.Create("ops.dispatch.rate_invalid");

    public static Error DestinationRequired { get; } = Error.Create("ops.dispatch.destination_required");

    public static Error ServiceRequired { get; } = Error.Create("ops.dispatch.service_required");

    public static Error TextInvalid { get; } = Error.Create("ops.dispatch.text_invalid");

    public static Error NotPlanned { get; } = Error.Create("ops.dispatch.not_planned");

    public static Error NotClosed { get; } = Error.Create("ops.dispatch.not_closed");

    public static Error NothingToClose { get; } = Error.Create("ops.dispatch.nothing_to_close");

    public static Error BundleNotClosed { get; } = Error.Create("ops.dispatch.bundle_not_closed");

    public static Error BundleElsewhere { get; } = Error.Create("ops.dispatch.bundle_elsewhere");

    public static Error WrongDestination { get; } = Error.Create("ops.dispatch.wrong_destination");

    public static Error WrongService { get; } = Error.Create("ops.dispatch.wrong_service");

    public static Error WrongWarehouse { get; } = Error.Create("ops.dispatch.wrong_warehouse");

    public static Error LegNotFound { get; } = Error.Create("ops.dispatch.leg_not_found");

    public static Error LegOrder { get; } = Error.Create("ops.dispatch.leg_order");

    public static Error LegTimeInvalid { get; } = Error.Create("ops.dispatch.leg_time_invalid");

    public static Error TooManyLegs { get; } = Error.Create("ops.dispatch.too_many_legs");

    public static Error CannotCancel { get; } = Error.Create("ops.dispatch.cannot_cancel");

    public static Error ReasonRequired { get; } = Error.Create("ops.dispatch.reason_required");

    public static Error ParcelHeld { get; } = Error.Create("ops.dispatch.parcel_held");

    public static Error TooManyBundles { get; } = Error.Create("ops.dispatch.too_many_bundles");

    public static Error CostInvalid { get; } = Error.Create("ops.dispatch.cost_invalid");

    public static Error UnknownCode { get; } = Error.Create("ops.dispatch.unknown_code");
}
