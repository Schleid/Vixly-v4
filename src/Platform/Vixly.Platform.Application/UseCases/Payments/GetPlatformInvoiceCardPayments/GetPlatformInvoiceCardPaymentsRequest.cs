namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformInvoiceCardPayments;

// =========================================================
// FILE: GetPlatformInvoiceCardPaymentsRequest.cs
//
// PURPOSE:
// Reads the card side of an invoice: transactions, the card that would be charged and what can be refunded.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformInvoiceCardPaymentsRequest
{
    public Guid PlatformInvoiceId { get; init; }
}
