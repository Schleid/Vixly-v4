using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSalesFeedRepository.cs
//
// PURPOSE:
// Stores the sales feed to TechOS and finds the history rows not yet in it.
//
// RESPONSIBILITIES:
// - Find invoice and subscription history rows with no feed event yet, oldest first
// - Add feed events; list pending ones by sequence; find one by id
// - Give the daily totals' inputs and the Control view's counts
// - Hold the feed's one state row
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// - The feed event table has one row per source row (unique index), so a sale is written once
// =========================================================
public interface IPlatformSalesFeedRepository
{
    Task<IReadOnlyList<PlatformInvoiceEvent>> ListUnfedInvoiceEventsAsync(IReadOnlyCollection<string> codes, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformSubscriptionEvent>> ListUnfedSubscriptionEventsAsync(IReadOnlyCollection<string> codes, int take, CancellationToken cancellationToken);

    Task<long> LastSequenceAsync(CancellationToken cancellationToken);

    void Add(PlatformSalesFeedEvent feedEvent);

    Task<IReadOnlyList<PlatformSalesFeedEvent>> ListPendingAsync(int take, CancellationToken cancellationToken);

    Task<PlatformSalesFeedEvent?> GetAsync(Guid eventId, CancellationToken cancellationToken);

    Task<PlatformInvoiceEvent?> FindInvoiceEventAsync(Guid rowId, CancellationToken cancellationToken);

    Task<PlatformSubscriptionEvent?> FindSubscriptionEventAsync(Guid rowId, CancellationToken cancellationToken);

    // The UTC day of the oldest sale event (not totals); null when there is none.
    Task<DateOnly?> FirstSaleDayAsync(CancellationToken cancellationToken);

    Task<DateOnly?> LastTotalsDayAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformSalesFeedDayItem>> ListDayAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    Task<int> CountAsync(PlatformSalesFeedStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformSalesFeedEvent>> ListParkedAsync(int take, CancellationToken cancellationToken);

    Task<PlatformSalesFeedState?> GetStateAsync(CancellationToken cancellationToken);

    void AddState(PlatformSalesFeedState state);
}
