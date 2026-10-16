using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Collections;

// =========================================================
// FILE: PlatformTenantCollectionsSummary.cs
//
// PURPOSE:
// The Collections section of a tenant page.
//
// RESPONSIBILITIES:
// - Carry the open case, if any, the tenant's overdue invoices and what the reader may do
//
// IMPORTANT NOTES:
// - No open case: the section says the tenant is up to date
// =========================================================
public sealed record PlatformTenantCollectionsSummary(
    PlatformCollectionCaseItem? OpenCase,
    IReadOnlyList<PlatformOverdueInvoiceItem> Overdue,
    PlatformCollectionCaseAbilities Abilities);
