using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsShippingServiceErrors.cs
//
// PURPOSE:
// Error codes of the shipping-service setup (Slice 1E, D-O15).
//
// RESPONSIBILITIES:
// - Name every refusal of a shipping-service change
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsShippingServiceErrors
{
    public static Error ServiceUnknown { get; } = Error.Create("ops.services.unknown");

    public static Error LabelTooLong { get; } = Error.Create("ops.services.label_too_long", ("maxLength", OperationsShippingServiceSetup.LabelMaxLength));

    public static Error DaysInvalid { get; } = Error.Create("ops.services.days_invalid");

    public static Error DivisorInvalid { get; } = Error.Create("ops.services.divisor_invalid");
}
