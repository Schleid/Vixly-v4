using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformTenantChangeRepository.cs
//
// PURPOSE:
// Stores and reads company change requests (B12).
//
// RESPONSIBILITIES:
// - Add and load requests for a decision
// - List and read them with the tenant, the decider and the proof’s name, never its bytes
// - Load a proof for download
//
// IMPORTANT NOTES:
// - GetAsync and GetPendingAsync do not load the proof
// - Lists read without tracking
// =========================================================
internal sealed class PlatformTenantChangeRepository : IPlatformTenantChangeRepository
{
    private readonly PlatformDbContext _context;

    public PlatformTenantChangeRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformTenantChangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _context.TenantChangeRequests.Add(request);
    }

    public Task<PlatformTenantChangeRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return _context.TenantChangeRequests.FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken);
    }

    public Task<PlatformTenantChangeRequest?> GetPendingAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.TenantChangeRequests.FirstOrDefaultAsync(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformTenantChangeStatus.Pending, cancellationToken);
    }

    public async Task<PlatformTenantChangeItem?> GetItemAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return (await Project(_context.TenantChangeRequests.AsNoTracking().Where(item => item.Id == requestId)).ToListAsync(cancellationToken)).Select(Map).FirstOrDefault();
    }

    public async Task<PlatformTenantChangeItem?> GetLatestItemAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        IQueryable<PlatformTenantChangeRequest> latest = _context.TenantChangeRequests.AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId)
            .OrderByDescending(item => item.CreatedUtc)
            .Take(1);

        return (await Project(latest).ToListAsync(cancellationToken)).Select(Map).FirstOrDefault();
    }

    public async Task<PagedResult<PlatformTenantChangeItem>> ListAsync(PlatformTenantChangeFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformTenantChangeRequest> requests = _context.TenantChangeRequests.AsNoTracking();

        if (filter.Status is { } status)
        {
            requests = requests.Where(item => item.Status == status);
        }

        if (filter.PlatformTenantId is { } tenantId)
        {
            requests = requests.Where(item => item.PlatformTenantId == tenantId);
        }

        int total = await requests.CountAsync(cancellationToken);
        var rows = await Project(requests.OrderByDescending(item => item.CreatedUtc).Skip(page.Skip).Take(page.PageSize)).ToListAsync(cancellationToken);

        return new PagedResult<PlatformTenantChangeItem>(rows.Select(Map).ToArray(), page, total);
    }

    public async Task<PlatformTenantChangeProofFile?> GetProofAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _context.TenantChangeProofs.AsNoTracking()
            .Where(item => item.PlatformTenantChangeRequestId == requestId)
            .Select(item => new PlatformTenantChangeProofFile(item.FileName, item.ContentType, item.Content))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Only the proof's name, type and size are read: lists never load the document.
    private IQueryable<Row> Project(IQueryable<PlatformTenantChangeRequest> requests)
    {
        return
            from request in requests
            join tenant in _context.Tenants.AsNoTracking() on request.PlatformTenantId equals tenant.Id
            join proof in _context.TenantChangeProofs.AsNoTracking() on request.Id equals proof.PlatformTenantChangeRequestId
            join user in _context.Users.AsNoTracking() on request.DecidedByUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            select new Row(
                request.Id,
                request.PlatformTenantId,
                tenant.TenantCode,
                tenant.Company.TradingName ?? tenant.Company.LegalName,
                request.Status,
                request.Previous,
                request.Proposed,
                request.Reason,
                request.SubmittedByName,
                request.CreatedUtc,
                request.DecidedUtc,
                user == null ? null : user.FirstName + " " + user.LastName,
                request.DecisionNote,
                proof.FileName,
                proof.ContentType,
                proof.SizeBytes);
    }

    private static PlatformTenantChangeItem Map(Row row)
    {
        return new PlatformTenantChangeItem(
            row.RequestId,
            row.PlatformTenantId,
            row.TenantCode,
            row.TenantName,
            row.Status,
            PlatformTenantCompanyItem.From(row.Previous),
            PlatformTenantCompanyItem.From(row.Proposed),
            row.Reason,
            row.SubmittedByName,
            row.SubmittedUtc,
            row.DecidedUtc,
            row.DecidedByName,
            row.DecisionNote,
            row.ProofFileName,
            row.ProofContentType,
            row.ProofSizeBytes);
    }

    private sealed record Row(
        Guid RequestId,
        Guid PlatformTenantId,
        string TenantCode,
        string TenantName,
        PlatformTenantChangeStatus Status,
        PlatformCompanyDetails Previous,
        PlatformCompanyDetails Proposed,
        string Reason,
        string SubmittedByName,
        DateTimeOffset SubmittedUtc,
        DateTimeOffset? DecidedUtc,
        string? DecidedByName,
        string? DecisionNote,
        string ProofFileName,
        string ProofContentType,
        int ProofSizeBytes);
}
