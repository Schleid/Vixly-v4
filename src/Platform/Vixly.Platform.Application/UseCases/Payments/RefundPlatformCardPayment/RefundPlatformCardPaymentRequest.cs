namespace Vixly.Platform.Application.UseCases.Payments.RefundPlatformCardPayment;

// =========================================================
// FILE: RefundPlatformCardPaymentRequest.cs
//
// PURPOSE:
// Refunds money to the card against a credit note the sales manager approved (D-P6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RefundPlatformCardPaymentRequest
{
    public Guid PlatformInvoiceId { get; init; }

    public Guid CreditId { get; init; }

    public decimal Amount { get; init; }
}
