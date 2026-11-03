using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardChargeRequest.cs
//
// PURPOSE:
// A charge of a saved card without the cardholder present.
//
// RESPONSIBILITIES:
// - Carry a charge of a saved card without the cardholder present
//
// IMPORTANT NOTES:
// - Plain data; no behaviour
// - StatementSuffix comes from the merchant account settings; null means none
// =========================================================
public sealed record PlatformCardChargeRequest(
    string CustomerId,
    string PaymentMethodId,
    long AmountMinor,
    string CurrencyCode,
    string Description,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey,
    string? StatementSuffix = null);
