using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSalesLeadRepository.cs
//
// PURPOSE:
// Stores and lists contact requests (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Add, load, list and prune leads
//
// IMPORTANT NOTES:
// - Lists read without tracking
// =========================================================
public interface IPlatformSalesLeadRepository
{
    void Add(PlatformSalesLead lead);

    Task<PlatformSalesLead?> GetAsync(Guid leadId, CancellationToken cancellationToken);

    Task<PlatformSalesLeadItem?> GetItemAsync(Guid leadId, CancellationToken cancellationToken);

    Task<PagedResult<PlatformSalesLeadItem>> ListAsync(PlatformSalesLeadFilter filter, PagedRequest page, CancellationToken cancellationToken);

    Task<int> DeleteClosedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
}
