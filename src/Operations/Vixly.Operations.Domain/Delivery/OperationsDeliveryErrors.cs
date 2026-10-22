using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsDeliveryErrors.cs
//
// PURPOSE:
// Error codes of home delivery (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsDeliveryErrors
{
    public static Error NotFound { get; } = Error.Create("ops.delivery.not_found");

    public static Error SettingsInvalid { get; } = Error.Create("ops.delivery.settings_invalid");

    public static Error DriverRequired { get; } = Error.Create("ops.delivery.driver_required");

    public static Error DateInvalid { get; } = Error.Create("ops.delivery.date_invalid");

    public static Error NotPlanned { get; } = Error.Create("ops.delivery.not_planned");

    public static Error NotOut { get; } = Error.Create("ops.delivery.not_out");

    public static Error Empty { get; } = Error.Create("ops.delivery.empty");

    public static Error TooManyStops { get; } = Error.Create("ops.delivery.too_many_stops");

    public static Error NotDeliverable { get; } = Error.Create("ops.delivery.not_deliverable");

    public static Error OnAnotherRoute { get; } = Error.Create("ops.delivery.on_another_route");

    public static Error StopDone { get; } = Error.Create("ops.delivery.stop_done");

    public static Error ReceiverRequired { get; } = Error.Create("ops.delivery.receiver_required");

    public static Error ReasonRequired { get; } = Error.Create("ops.delivery.reason_required");

    public static Error CashInvalid { get; } = Error.Create("ops.delivery.cash_invalid");

    public static Error StopsPending { get; } = Error.Create("ops.delivery.stops_pending");

    public static Error NotReturned { get; } = Error.Create("ops.delivery.not_returned");

    public static Error CashNotPaidIn { get; } = Error.Create("ops.delivery.cash_not_paid_in");

    public static Error NotYourRoute { get; } = Error.Create("ops.delivery.not_your_route");
}
