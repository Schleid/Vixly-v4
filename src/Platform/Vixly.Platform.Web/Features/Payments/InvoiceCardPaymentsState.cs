using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: InvoiceCardPaymentsState.cs
//
// PURPOSE:
// What the Card payments panel of an invoice shows.
//
// RESPONSIBILITIES:
// - Hold transactions, the card that would be charged, refundable credit notes, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record InvoiceCardPaymentsState(PlatformInvoiceCardPaymentsItem? Payments, bool Loaded, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static InvoiceCardPaymentsState Initial { get; } = new(null, false, false, null, null);
}
