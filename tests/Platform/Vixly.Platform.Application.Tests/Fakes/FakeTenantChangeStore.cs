using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeTenantChangeStore.cs
//
// PURPOSE:
// In-memory company change requests for application tests (B12).
//
// RESPONSIBILITIES:
// - Implement the change request port with a list
//
// IMPORTANT NOTES:
// - Projections read the tenant list for names; deciders show as "Staff"
// =========================================================
internal sealed class FakeTenantChangeStore : IPlatformTenantChangeRepository
{
    private readonly FakeTenantStore _tenants;

    public FakeTenantChangeStore(FakeTenantStore tenants)
    {
        _tenants = tenants;
    }

    public List<PlatformTenantChangeRequest> Requests { get; } = new();

    public void Add(PlatformTenantChangeRequest request)
    {
        Requests.Add(request);
    }

    public Task<PlatformTenantChangeRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Requests.FirstOrDefault(request => request.Id == requestId));
    }

    public Task<PlatformTenantChangeRequest?> GetPendingAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Requests.FirstOrDefault(request => request.PlatformTenantId == platformTenantId && request.IsPending));
    }

    public Task<PlatformTenantChangeItem?> GetItemAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Requests.Where(request => request.Id == requestId).Select(Item).FirstOrDefault());
    }

    public Task<PlatformTenantChangeItem?> GetLatestItemAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Requests.Where(request => request.PlatformTenantId == platformTenantId).OrderByDescending(request => request.CreatedUtc).Select(Item).FirstOrDefault());
    }

    public Task<PagedResult<PlatformTenantChangeItem>> ListAsync(PlatformTenantChangeFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        List<PlatformTenantChangeItem> items = Requests
            .Where(request => filter.Status is null || request.Status == filter.Status)
            .Where(request => filter.PlatformTenantId is null || request.PlatformTenantId == filter.PlatformTenantId)
            .Select(Item)
            .ToList();
        return Task.FromResult(new PagedResult<PlatformTenantChangeItem>(items.Skip(page.Skip).Take(page.PageSize).ToList(), page, items.Count));
    }

    public Task<PlatformTenantChangeProofFile?> GetProofAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Requests.Where(request => request.Id == requestId)
            .Select(request => new PlatformTenantChangeProofFile(request.Proof.FileName, request.Proof.ContentType, request.Proof.Content))
            .FirstOrDefault());
    }

    private PlatformTenantChangeItem Item(PlatformTenantChangeRequest request)
    {
        PlatformTenant tenant = _tenants.Tenants.First(item => item.Id == request.PlatformTenantId);
        return new PlatformTenantChangeItem(
            request.Id, tenant.Id, tenant.TenantCode, tenant.Company.DisplayName, request.Status,
            PlatformTenantCompanyItem.From(request.Previous), PlatformTenantCompanyItem.From(request.Proposed),
            request.Reason, request.SubmittedByName, request.CreatedUtc, request.DecidedUtc,
            request.DecidedByUserId is null ? null : "Staff", request.DecisionNote,
            request.Proof.FileName, request.Proof.ContentType, request.Proof.SizeBytes);
    }
}
