using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: IPlatformBillingQueries.cs
//
// PURPOSE:
// Read models for invoices (Slice 4).
//
// RESPONSIBILITIES:
// - Paged invoice list and a tenant balance summary
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure with no tracking; every list is paged (P-1)
// =========================================================
public interface IPlatformBillingQueries
{
    Task<PagedResult<PlatformInvoiceListItem>> ListInvoicesAsync(
        PlatformInvoiceListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<PlatformBillingDocumentItem>> ListDocumentsAsync(Guid platformTenantId, PagedRequest page, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformTenantBillingSummary>> SummaryAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken);
}
