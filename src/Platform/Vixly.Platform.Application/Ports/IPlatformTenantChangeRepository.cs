using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformTenantChangeRepository.cs
//
// PURPOSE:
// Stores company change requests and their proofs (B12).
//
// RESPONSIBILITIES:
// - Add and load requests
// - List and read them without the proof bytes
// - Load a proof for download
//
// IMPORTANT NOTES:
// - One pending request per tenant is also enforced by a filtered unique index
// =========================================================
public interface IPlatformTenantChangeRepository
{
    void Add(PlatformTenantChangeRequest request);

    Task<PlatformTenantChangeRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken);

    Task<PlatformTenantChangeRequest?> GetPendingAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PlatformTenantChangeItem?> GetItemAsync(Guid requestId, CancellationToken cancellationToken);

    Task<PlatformTenantChangeItem?> GetLatestItemAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<PagedResult<PlatformTenantChangeItem>> ListAsync(PlatformTenantChangeFilter filter, PagedRequest page, CancellationToken cancellationToken);

    Task<PlatformTenantChangeProofFile?> GetProofAsync(Guid requestId, CancellationToken cancellationToken);
}
