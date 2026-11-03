using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.SalesFeed;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.SalesFeed;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeSalesFeedStore.cs
//
// PURPOSE:
// In-memory sales feed store, TechOS client and alert mailer for tests.
//
// RESPONSIBILITIES:
// - Read the history rows from the billing and subscription fakes
// - Answer each POST with the next scripted answer and keep the bodies
//
// IMPORTANT NOTES:
// - Test double only; the unique source rule is kept like the database index
// =========================================================
internal sealed class FakeSalesFeedStore : IPlatformSalesFeedRepository, IPlatformSalesFeedClient, IPlatformSalesFeedMailer
{
    private readonly List<PlatformInvoiceEvent> _invoiceEvents;

    private readonly List<PlatformSubscriptionEvent> _subscriptionEvents;

    public FakeSalesFeedStore(List<PlatformInvoiceEvent> invoiceEvents, List<PlatformSubscriptionEvent> subscriptionEvents)
    {
        _invoiceEvents = invoiceEvents;
        _subscriptionEvents = subscriptionEvents;
    }

    public List<PlatformSalesFeedEvent> Events { get; } = new();

    public PlatformSalesFeedState? State { get; set; }

    public bool Configured { get; set; } = true;

    public string? SettingsProblem => Configured ? null : "url_missing";

    public Queue<Func<string, PlatformSalesFeedAnswer>> Answers { get; } = new();

    public List<string> Bodies { get; } = new();

    public List<(string Email, PlatformSalesFeedAlert Alert)> Alerts { get; } = new();

    public Task<IReadOnlyList<PlatformInvoiceEvent>> ListUnfedInvoiceEventsAsync(IReadOnlyCollection<string> codes, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformInvoiceEvent>>(_invoiceEvents
            .Where(row => codes.Contains(row.EventCode) && !Events.Any(fed => fed.Source == PlatformSalesFeedSource.InvoiceEvent && fed.SourceId == row.Id))
            .OrderBy(row => row.CreatedUtc).ThenBy(row => row.Id).Take(take).ToArray());
    }

    public Task<IReadOnlyList<PlatformSubscriptionEvent>> ListUnfedSubscriptionEventsAsync(IReadOnlyCollection<string> codes, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformSubscriptionEvent>>(_subscriptionEvents
            .Where(row => codes.Contains(row.EventCode) && !Events.Any(fed => fed.Source == PlatformSalesFeedSource.SubscriptionEvent && fed.SourceId == row.Id))
            .OrderBy(row => row.CreatedUtc).ThenBy(row => row.Id).Take(take).ToArray());
    }

    public Task<long> LastSequenceAsync(CancellationToken cancellationToken) => Task.FromResult(Events.Count == 0 ? 0L : Events.Max(item => item.Sequence));

    public void Add(PlatformSalesFeedEvent feedEvent)
    {
        if (Events.Any(item => item.Sequence == feedEvent.Sequence
            || (feedEvent.SourceId is not null && item.Source == feedEvent.Source && item.SourceId == feedEvent.SourceId)
            || (feedEvent.TotalsDate is not null && item.TotalsDate == feedEvent.TotalsDate)))
        {
            throw new InvalidOperationException("Unique index.");
        }

        Events.Add(feedEvent);
    }

    public Task<IReadOnlyList<PlatformSalesFeedEvent>> ListPendingAsync(int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformSalesFeedEvent>>(Events.Where(item => item.Status == PlatformSalesFeedStatus.Pending).OrderBy(item => item.Sequence).Take(take).ToArray());
    }

    public Task<PlatformSalesFeedEvent?> GetAsync(Guid eventId, CancellationToken cancellationToken) => Task.FromResult(Events.FirstOrDefault(item => item.Id == eventId));

    public Task<PlatformInvoiceEvent?> FindInvoiceEventAsync(Guid rowId, CancellationToken cancellationToken) => Task.FromResult(_invoiceEvents.FirstOrDefault(row => row.Id == rowId));

    public Task<PlatformSubscriptionEvent?> FindSubscriptionEventAsync(Guid rowId, CancellationToken cancellationToken) => Task.FromResult(_subscriptionEvents.FirstOrDefault(row => row.Id == rowId));

    public Task<DateOnly?> FirstSaleDayAsync(CancellationToken cancellationToken)
    {
        PlatformSalesFeedEvent[] sales = Events.Where(item => item.Source != PlatformSalesFeedSource.DailyTotals).ToArray();
        return Task.FromResult<DateOnly?>(sales.Length == 0 ? null : DateOnly.FromDateTime(sales.Min(item => item.OccurredUtc).UtcDateTime));
    }

    public Task<DateOnly?> LastTotalsDayAsync(CancellationToken cancellationToken) => Task.FromResult(Events.Where(item => item.TotalsDate != null).Max(item => item.TotalsDate));

    public Task<IReadOnlyList<PlatformSalesFeedDayItem>> ListDayAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformSalesFeedDayItem>>(Events
            .Where(item => item.Source != PlatformSalesFeedSource.DailyTotals && item.Status != PlatformSalesFeedStatus.Replaced && item.OccurredUtc >= fromUtc && item.OccurredUtc < toUtc)
            .Select(item => new PlatformSalesFeedDayItem(item.EventType, item.Amount, item.CurrencyCode)).ToArray());
    }

    public Task<int> CountAsync(PlatformSalesFeedStatus status, CancellationToken cancellationToken) => Task.FromResult(Events.Count(item => item.Status == status));

    public Task<IReadOnlyList<PlatformSalesFeedEvent>> ListParkedAsync(int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformSalesFeedEvent>>(Events.Where(item => item.Status == PlatformSalesFeedStatus.Parked).OrderByDescending(item => item.Sequence).Take(take).ToArray());
    }

    public Task<PlatformSalesFeedState?> GetStateAsync(CancellationToken cancellationToken) => Task.FromResult(State);

    public void AddState(PlatformSalesFeedState state) => State = state;

    public Task<PlatformSalesFeedAnswer> SendAsync(string body, CancellationToken cancellationToken)
    {
        Bodies.Add(body);
        return Task.FromResult(Answers.Count == 0 ? PlatformSalesFeedAnswer.Of(PlatformSalesFeedAnswerKind.Retry, "no_answer") : Answers.Dequeue()(body));
    }

    public Task SendAlertAsync(PlatformPermissionHolderItem recipient, PlatformSalesFeedAlert alert, CancellationToken cancellationToken)
    {
        Alerts.Add((recipient.Email, alert));
        return Task.CompletedTask;
    }

    // Accepts every event of the body, as TechOS does.
    public static PlatformSalesFeedAnswer AcceptAll(string body)
    {
        return new PlatformSalesFeedAnswer(PlatformSalesFeedAnswerKind.Read, "http_200", Ids(body), Array.Empty<Guid>(), Array.Empty<PlatformSalesFeedRejection>());
    }

    public static IReadOnlyList<Guid> Ids(string body)
    {
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(body);
        return document.RootElement.GetProperty("events").EnumerateArray().Select(item => Guid.Parse(item.GetProperty("id").GetString()!)).ToArray();
    }
}
