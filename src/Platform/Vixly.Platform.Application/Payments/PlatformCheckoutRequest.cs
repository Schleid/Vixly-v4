using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCheckoutRequest.cs
//
// PURPOSE:
// What a hosted checkout needs: customer, amount from the server, return pages and metadata.
//
// RESPONSIBILITIES:
// - Carry what a hosted checkout needs: customer, amount from the server, return pages and metadata
//
// IMPORTANT NOTES:
// - AmountMinor is ignored for SaveCard
// - The amount is computed by the server (D-P7)
// =========================================================
public sealed record PlatformCheckoutRequest(
    PlatformCheckoutPurpose Purpose,
    string CustomerId,
    long AmountMinor,
    string CurrencyCode,
    string Description,
    bool SaveCard,
    IReadOnlyDictionary<string, string> Metadata,
    string SuccessUrl,
    string CancelUrl,
    string Language,
    string IdempotencyKey);
