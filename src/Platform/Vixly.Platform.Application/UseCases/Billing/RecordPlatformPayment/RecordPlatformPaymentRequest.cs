using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing.RecordPlatformPayment;

// =========================================================
// FILE: RecordPlatformPaymentRequest.cs
//
// PURPOSE:
// Records money received by hand against an invoice and issues its receipt (D-B7, D-B11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordPlatformPaymentRequest
{
    public Guid PlatformInvoiceId { get; init; }

    public decimal Amount { get; init; }

    public PlatformPaymentMethod Method { get; init; }

    public DateTimeOffset ReceivedUtc { get; init; }

    public string? Reference { get; init; }

    public string? Note { get; init; }
}
