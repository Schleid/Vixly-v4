namespace Vixly.Platform.Application.UseCases.Billing.ReversePlatformPayment;

// =========================================================
// FILE: ReversePlatformPaymentRequest.cs
//
// PURPOSE:
// Reverses a recorded payment (bounced cheque, wrong entry); the balance reopens (D-B7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReversePlatformPaymentRequest
{
    public Guid PlatformInvoiceId { get; init; }

    public Guid PaymentId { get; init; }

    public string? Reason { get; init; }
}
