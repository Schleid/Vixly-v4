using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSalesFeedRepository.cs
//
// PURPOSE:
// Stores the sales feed to TechOS (EF Core).
//
// RESPONSIBILITIES:
// - Implement IPlatformSalesFeedRepository over the platform schema
//
// IMPORTANT NOTES:
// - History rows not yet fed are found by an anti-join on (Source, SourceId)
// - Reads for the job and the Control page are not tracked; events to update are
// =========================================================
internal sealed class PlatformSalesFeedRepository : IPlatformSalesFeedRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSalesFeedRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PlatformInvoiceEvent>> ListUnfedInvoiceEventsAsync(IReadOnlyCollection<string> codes, int take, CancellationToken cancellationToken)
    {
        string[] wanted = codes.ToArray();

        return await _context.InvoiceEvents.AsNoTracking()
            .Where(row => wanted.Contains(row.EventCode)
                && !_context.SalesFeedEvents.Any(fed => fed.Source == PlatformSalesFeedSource.InvoiceEvent && fed.SourceId == row.Id))
            .OrderBy(row => row.CreatedUtc)
            .ThenBy(row => row.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformSubscriptionEvent>> ListUnfedSubscriptionEventsAsync(IReadOnlyCollection<string> codes, int take, CancellationToken cancellationToken)
    {
        string[] wanted = codes.ToArray();

        return await _context.SubscriptionEvents.AsNoTracking()
            .Where(row => wanted.Contains(row.EventCode)
                && !_context.SalesFeedEvents.Any(fed => fed.Source == PlatformSalesFeedSource.SubscriptionEvent && fed.SourceId == row.Id))
            .OrderBy(row => row.CreatedUtc)
            .ThenBy(row => row.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> LastSequenceAsync(CancellationToken cancellationToken)
    {
        return await _context.SalesFeedEvents.MaxAsync(item => (long?)item.Sequence, cancellationToken) ?? 0L;
    }

    public void Add(PlatformSalesFeedEvent feedEvent)
    {
        ArgumentNullException.ThrowIfNull(feedEvent);

        _context.SalesFeedEvents.Add(feedEvent);
    }

    public async Task<IReadOnlyList<PlatformSalesFeedEvent>> ListPendingAsync(int take, CancellationToken cancellationToken)
    {
        return await _context.SalesFeedEvents
            .Where(item => item.Status == PlatformSalesFeedStatus.Pending)
            .OrderBy(item => item.Sequence)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<PlatformSalesFeedEvent?> GetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return _context.SalesFeedEvents.FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken);
    }

    public Task<PlatformInvoiceEvent?> FindInvoiceEventAsync(Guid rowId, CancellationToken cancellationToken)
    {
        return _context.InvoiceEvents.AsNoTracking().FirstOrDefaultAsync(row => row.Id == rowId, cancellationToken);
    }

    public Task<PlatformSubscriptionEvent?> FindSubscriptionEventAsync(Guid rowId, CancellationToken cancellationToken)
    {
        return _context.SubscriptionEvents.AsNoTracking().FirstOrDefaultAsync(row => row.Id == rowId, cancellationToken);
    }

    public async Task<DateOnly?> FirstSaleDayAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset? first = await _context.SalesFeedEvents.AsNoTracking()
            .Where(item => item.Source != PlatformSalesFeedSource.DailyTotals)
            .MinAsync(item => (DateTimeOffset?)item.OccurredUtc, cancellationToken);
        return first is { } value ? DateOnly.FromDateTime(value.UtcDateTime) : null;
    }

    public Task<DateOnly?> LastTotalsDayAsync(CancellationToken cancellationToken)
    {
        return _context.SalesFeedEvents.AsNoTracking()
            .Where(item => item.TotalsDate != null)
            .MaxAsync(item => item.TotalsDate, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformSalesFeedDayItem>> ListDayAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        return await _context.SalesFeedEvents.AsNoTracking()
            .Where(item => item.Source != PlatformSalesFeedSource.DailyTotals && item.Status != PlatformSalesFeedStatus.Replaced && item.OccurredUtc >= fromUtc && item.OccurredUtc < toUtc)
            .Select(item => new PlatformSalesFeedDayItem(item.EventType, item.Amount, item.CurrencyCode))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(PlatformSalesFeedStatus status, CancellationToken cancellationToken)
    {
        return _context.SalesFeedEvents.CountAsync(item => item.Status == status, cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformSalesFeedEvent>> ListParkedAsync(int take, CancellationToken cancellationToken)
    {
        return await _context.SalesFeedEvents.AsNoTracking()
            .Where(item => item.Status == PlatformSalesFeedStatus.Parked)
            .OrderByDescending(item => item.Sequence)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<PlatformSalesFeedState?> GetStateAsync(CancellationToken cancellationToken)
    {
        return _context.SalesFeedStates.FirstOrDefaultAsync(item => item.Key == PlatformSalesFeedLimits.SingletonKey, cancellationToken);
    }

    public void AddState(PlatformSalesFeedState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _context.SalesFeedStates.Add(state);
    }
}
