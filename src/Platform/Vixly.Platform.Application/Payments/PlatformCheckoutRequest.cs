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
// - AllowBankAccount adds a US bank account debit next to cards; the adapter drops it where the provider cannot take it
// - Branding and StatementSuffix come from the merchant account settings; null or empty means the provider's own
// - CardOnly hides the provider's own wallet (Stripe Link and its bank option): registration takes cards, Apple Pay and Google Pay only (owner, 2026-11-02)
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
    string IdempotencyKey,
    bool AllowBankAccount = false,
    PlatformCheckoutBranding? Branding = null,
    string? StatementSuffix = null,
    bool CardOnly = false);
