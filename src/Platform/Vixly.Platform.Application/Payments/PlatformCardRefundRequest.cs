using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardRefundRequest.cs
//
// PURPOSE:
// A refund of part or all of a card payment.
//
// RESPONSIBILITIES:
// - Carry a refund of part or all of a card payment
//
// IMPORTANT NOTES:
// - Plain data; no behaviour
// =========================================================
public sealed record PlatformCardRefundRequest(
    string PaymentId,
    long AmountMinor,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey);
