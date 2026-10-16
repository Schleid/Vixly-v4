using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformCollectionsRepository.cs
//
// PURPOSE:
// Stores collections settings, cases and history.
//
// RESPONSIBILITIES:
// - Look in rows of the current transaction first, so one commit sees what it added
// - List the tenants the clock should look at, by id
//
// IMPORTANT NOTES:
// - History rows are only added, never changed
// =========================================================
internal sealed class PlatformCollectionsRepository : IPlatformCollectionsRepository
{
    private readonly PlatformDbContext _context;

    public PlatformCollectionsRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlatformCollectionsSettings?> FindSettingsAsync(CancellationToken cancellationToken)
    {
        return _context.CollectionsSettings.Local.FirstOrDefault(item => item.Key == PlatformCollectionsLimits.SingletonKey)
            ?? await _context.CollectionsSettings.FirstOrDefaultAsync(item => item.Key == PlatformCollectionsLimits.SingletonKey, cancellationToken);
    }

    public void AddSettings(PlatformCollectionsSettings settings) => _context.CollectionsSettings.Add(settings);

    public async Task<PlatformCollectionCase?> GetOpenCaseAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return _context.CollectionCases.Local.FirstOrDefault(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformCollectionCaseStatus.Open)
            ?? await _context.CollectionCases.FirstOrDefaultAsync(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformCollectionCaseStatus.Open, cancellationToken);
    }

    public Task<PlatformCollectionCase?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return _context.CollectionCases.FirstOrDefaultAsync(item => item.Id == caseId, cancellationToken);
    }

    public void AddCase(PlatformCollectionCase collectionCase) => _context.CollectionCases.Add(collectionCase);

    public void AddEvents(IEnumerable<PlatformCollectionCaseEvent> events) => _context.CollectionCaseEvents.AddRange(events);

    public Task<bool> RetryRecordedAsync(string retryKey, CancellationToken cancellationToken)
    {
        return _context.CollectionCaseEvents.AsNoTracking().AnyAsync(item => item.RetryKey == retryKey, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListTenantsToCheckAsync(DateTimeOffset utcNow, Guid? after, int take, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);
        IQueryable<Guid> overdue = _context.Invoices.AsNoTracking()
            .Where(item => item.Status == PlatformInvoiceStatus.Issued && item.Balance > 0m && item.DueUtc < cutoff)
            .Select(item => item.PlatformTenantId);
        IQueryable<Guid> open = _context.CollectionCases.AsNoTracking()
            .Where(item => item.Status == PlatformCollectionCaseStatus.Open)
            .Select(item => item.PlatformTenantId);
        IQueryable<Guid> unpaid = _context.Subscriptions.AsNoTracking()
            .Where(item => item.Status == PlatformSubscriptionStatus.PendingPayment || item.Status == PlatformSubscriptionStatus.BlockedForNonPayment || item.Status == PlatformSubscriptionStatus.PastDue)
            .Select(item => item.PlatformTenantId);
        IQueryable<Guid> tenants = overdue.Union(open).Union(unpaid);

        if (after is Guid last)
        {
            tenants = tenants.Where(id => id.CompareTo(last) > 0);
        }

        return await tenants.OrderBy(id => id).Take(take).ToListAsync(cancellationToken);
    }
}
