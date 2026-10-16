using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.Collections;

// =========================================================
// FILE: PlatformCollectionsMail.cs
//
// PURPOSE:
// What one collections email says: which step, the overdue invoices with pay links, and the dates that matter (D-C11).
//
// RESPONSIBILITIES:
// - Carry everything the mailer needs to write the email in the tenant's language
//
// IMPORTANT NOTES:
// - Never carries the internal reason of an extension or a note
// - PaymentInstructions come from Billing settings in the tenant's language
// =========================================================
public sealed record PlatformCollectionsMail(
    PlatformCollectionsNoticeKind Kind,
    IReadOnlyList<PlatformCollectionsMailInvoice> Invoices,
    IReadOnlyList<PlatformMoneyItem> Totals,
    DateTimeOffset? BlockDateUtc,
    DateTime? ExtendedThrough,
    string? PaymentInstructions);
