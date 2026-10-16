using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformBillingDocumentIssued.cs
//
// PURPOSE:
// Outbox payload: an invoice, credit note or receipt was issued and may be emailed (D-B13).
//
// RESPONSIBILITIES:
// - Name the document to send
//
// IMPORTANT NOTES:
// - Versioned with the message type; add fields, never rename
// =========================================================
public sealed record PlatformBillingDocumentIssued(
    PlatformBillingDocumentKind Kind,
    Guid PlatformInvoiceId,
    Guid OwnerId,
    Guid PlatformTenantId,
    DateTimeOffset OccurredUtc);
