namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformInvoicePaid.cs
//
// PURPOSE:
// Outbox payload: an invoice is fully paid (for Slices 6 and 7 and other planes).
//
// RESPONSIBILITIES:
// - Say which invoice of which tenant and subscription is paid
//
// IMPORTANT NOTES:
// - Versioned with the message type; add fields, never rename
// =========================================================
public sealed record PlatformInvoicePaid(
    Guid PlatformInvoiceId,
    string Number,
    Guid PlatformTenantId,
    Guid? PlatformSubscriptionId,
    decimal Total,
    string CurrencyCode,
    DateTimeOffset OccurredUtc);
