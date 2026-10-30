using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Cashier.StartOperationsCardPayment;

// =========================================================
// FILE: StartOperationsCardPaymentRequest.cs
//
// PURPOSE:
// Starts a card payment for an invoice at the cashier: Stripe's payment page (QR code or customer screen) or a card reader (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartOperationsCardPaymentRequest
{
    public Guid InvoiceId { get; init; }

    public OperationsCardChannel Channel { get; init; } = OperationsCardChannel.PaymentPage;

    public Guid? ReaderId { get; init; }

    // Where Stripe's page sends the customer back: this address followed by the charge's id.
    public string ReturnUrlPrefix { get; init; } = string.Empty;
}
