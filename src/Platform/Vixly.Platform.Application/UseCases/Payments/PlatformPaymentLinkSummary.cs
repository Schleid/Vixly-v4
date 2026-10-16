using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformPaymentLinkSummary.cs
//
// PURPOSE:
// What a pay or save-card link shows on the public page before the tenant continues.
//
// RESPONSIBILITIES:
// - Carry what the console or public page shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond brand, last four and expiry
// =========================================================
public sealed record PlatformPaymentLinkSummary(
    PlatformPaymentLinkKind Kind,
    string TenantName,
    string? InvoiceNumber,
    decimal? Balance,
    string? CurrencyCode,
    DateTimeOffset? DueUtc,
    bool Settled,
    bool CardPaymentsAvailable,
    string ConsentTextVersion,
    PlatformZelleOffer? Zelle = null);
