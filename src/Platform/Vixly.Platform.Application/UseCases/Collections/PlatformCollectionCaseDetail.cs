using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Collections;

// =========================================================
// FILE: PlatformCollectionCaseDetail.cs
//
// PURPOSE:
// A collections case with its overdue invoices, history and what the reader may do.
//
// RESPONSIBILITIES:
// - Carry the case page in one read
//
// IMPORTANT NOTES:
// - Invoices are the tenant's overdue invoices now, not when the case opened
// =========================================================
public sealed record PlatformCollectionCaseDetail(
    PlatformCollectionCaseItem Case,
    IReadOnlyList<PlatformOverdueInvoiceItem> Invoices,
    IReadOnlyList<PlatformCollectionEventItem> History,
    PlatformCollectionCaseAbilities Abilities);
