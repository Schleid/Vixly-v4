namespace Vixly.Contracts.Platform.Payments;

// =========================================================
// FILE: PlatformPaymentLinkInfo.cs
//
// PURPOSE:
// What a pay or save-card link is for, shown on the public page.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Kind is "pay_invoice" or "save_card"
// - Zelle is set only for open US-dollar invoices while Zelle is on
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformPaymentLinkInfo(
    string Kind,
    string TenantName,
    string? InvoiceNumber,
    decimal? Balance,
    string? CurrencyCode,
    DateTimeOffset? DueUtc,
    bool Settled,
    bool CardPaymentsAvailable,
    string ConsentTextVersion,
    PlatformZelleInfo? Zelle = null);
