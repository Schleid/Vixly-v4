using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformCollectionsQueries.cs
//
// PURPOSE:
// Read models for the collections screens (D-C13, D-C14).
//
// RESPONSIBILITIES:
// - Page the case list
// - Compute the dashboard with aggregate queries
// - Read a case's history and a tenant's overdue invoices
//
// IMPORTANT NOTES:
// - Never loads cases to count them (P-1)
// =========================================================
public interface IPlatformCollectionsQueries
{
    Task<PagedResult<PlatformCollectionCaseItem>> ListCasesAsync(PlatformCollectionCaseFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<PlatformCollectionCaseItem?> GetCaseAsync(Guid caseId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformCollectionEventItem>> ListEventsAsync(Guid caseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformOverdueInvoiceItem>> ListOverdueInvoicesAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<PlatformCollectionsDashboard> GetDashboardAsync(DateTimeOffset utcNow, int longBlockDays, CancellationToken cancellationToken);
}
