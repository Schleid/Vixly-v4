using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsPrintingErrors.cs
//
// PURPOSE:
// Error codes of label printers and the print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; texts live in the resources (L-1)
// =========================================================
public static class OperationsPrintingErrors
{
    public static Error NameInvalid { get; } = Error.Create("ops.printing.name_invalid");

    public static Error AddressInvalid { get; } = Error.Create("ops.printing.address_invalid");

    public static Error DpiInvalid { get; } = Error.Create("ops.printing.dpi_invalid");

    public static Error PrinterNotFound { get; } = Error.Create("ops.printing.printer_not_found");

    public static Error AgentNotFound { get; } = Error.Create("ops.printing.agent_not_found");

    // Slice 12E: a pairing code unknown, used or older than 10 minutes.
    public static Error PairingInvalid { get; } = Error.Create("ops.printing.pairing_invalid");

    // Slice 12E: a portable printer belongs to its phone; it is changed from there.
    public static Error PhonePrinter { get; } = Error.Create("ops.printing.phone_printer");

    // Slice 4D step 2: a shipping label only for a cleared parcel (checked, no hold, no inspection waiting, in one of the customer's shipments).
    public static Error NotCleared { get; } = Error.Create("ops.printing.not_cleared");

    // Owner, 2026-10-28 (option A): no tracking number, no shipping label; it prints once the shipment is paid, on account or collect.
    public static Error NotPaid { get; } = Error.Create("ops.printing.not_paid");
}
