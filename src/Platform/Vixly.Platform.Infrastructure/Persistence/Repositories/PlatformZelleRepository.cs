using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformZelleRepository.cs
//
// PURPOSE:
// EF Core store for the Zelle details and reports.
//
// RESPONSIBILITIES:
// - Load tracked rows for the use cases; answer the rule questions
//
// IMPORTANT NOTES:
// - Saved by the unit of work
// =========================================================
internal sealed class PlatformZelleRepository : IPlatformZelleRepository
{
    private readonly PlatformDbContext _context;

    public PlatformZelleRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformZelleAccount?> GetAccountAsync(CancellationToken cancellationToken)
    {
        return _context.ZelleAccounts.Local.FirstOrDefault(item => item.Key == PlatformPaymentsLimits.SingletonKey)
            ?? await _context.ZelleAccounts.FirstOrDefaultAsync(item => item.Key == PlatformPaymentsLimits.SingletonKey, cancellationToken);
    }

    public void AddAccount(PlatformZelleAccount account) => _context.ZelleAccounts.Add(account);

    public async Task<PlatformZelleClaim?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        return _context.ZelleClaims.Local.FirstOrDefault(item => item.Id == claimId)
            ?? await _context.ZelleClaims.FirstOrDefaultAsync(item => item.Id == claimId, cancellationToken);
    }

    public Task<PlatformZelleClaim?> FindWaitingAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return _context.ZelleClaims.FirstOrDefaultAsync(item => item.PlatformInvoiceId == invoiceId && item.Status == PlatformZelleClaimStatus.Waiting, cancellationToken);
    }

    public Task<int> CountForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return _context.ZelleClaims.CountAsync(item => item.PlatformInvoiceId == invoiceId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListWaitingInvoiceIdsAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return await _context.ZelleClaims.AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformZelleClaimStatus.Waiting)
            .Select(item => item.PlatformInvoiceId)
            .ToListAsync(cancellationToken);
    }

    public void AddClaim(PlatformZelleClaim claim) => _context.ZelleClaims.Add(claim);
}
