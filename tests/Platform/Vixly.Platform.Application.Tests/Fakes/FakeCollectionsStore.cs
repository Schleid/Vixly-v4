using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeCollectionsStore.cs
//
// PURPOSE:
// In-memory collections settings, cases, history, queries and mailer for the Slice 6 tests.
//
// RESPONSIBILITIES:
// - Keep settings, cases and history rows; enforce the unique retry key
// - Answer the read models from the same lists and from the billing invoices it is given
// - Record every email instead of sending it
//
// IMPORTANT NOTES:
// - Invoices come from the billing fake through the Invoices function
// =========================================================
internal sealed class FakeCollectionsStore : IPlatformCollectionsRepository, IPlatformCollectionsQueries, IPlatformCollectionsMailer
{
    public PlatformCollectionsSettings? Settings { get; set; }

    public List<PlatformCollectionCase> Cases { get; } = new();

    public List<PlatformCollectionCaseEvent> Events { get; } = new();

    public List<(string To, PlatformCollectionsMail Mail)> Mails { get; } = new();

    public Func<IEnumerable<PlatformInvoice>> Invoices { get; set; } = () => Array.Empty<PlatformInvoice>();

    public Func<IEnumerable<Guid>> UnpaidTenants { get; set; } = () => Array.Empty<Guid>();

    public Task<PlatformCollectionsSettings?> FindSettingsAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);

    public void AddSettings(PlatformCollectionsSettings settings) => Settings = settings;

    public Task<PlatformCollectionCase?> GetOpenCaseAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Cases.FirstOrDefault(item => item.PlatformTenantId == platformTenantId && item.IsOpen));
    }

    public Task<PlatformCollectionCase?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Cases.FirstOrDefault(item => item.Id == caseId));
    }

    public void AddCase(PlatformCollectionCase collectionCase) => Cases.Add(collectionCase);

    public void AddEvents(IEnumerable<PlatformCollectionCaseEvent> events)
    {
        foreach (PlatformCollectionCaseEvent item in events)
        {
            if (item.RetryKey is not null && Events.Any(other => other.RetryKey == item.RetryKey))
            {
                throw new InvalidOperationException("Duplicate retry key.");
            }

            Events.Add(item);
        }
    }

    public Task<bool> RetryRecordedAsync(string retryKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(Events.Any(item => item.RetryKey == retryKey));
    }

    public Task<IReadOnlyList<Guid>> ListTenantsToCheckAsync(DateTimeOffset utcNow, Guid? after, int take, CancellationToken cancellationToken)
    {
        IEnumerable<Guid> tenants = Invoices().Where(item => item.IsOverdueAt(utcNow)).Select(item => item.PlatformTenantId)
            .Union(Cases.Where(item => item.IsOpen).Select(item => item.PlatformTenantId))
            .Union(UnpaidTenants());

        return Task.FromResult<IReadOnlyList<Guid>>(tenants.Where(id => after is null || id.CompareTo(after.Value) > 0).OrderBy(id => id).Take(take).ToArray());
    }

    public Task<PagedResult<PlatformCollectionCaseItem>> ListCasesAsync(PlatformCollectionCaseFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformCollectionCaseItem[] items = Cases
            .Where(item => filter.Status is null || item.Status == filter.Status)
            .Where(item => filter.Stage is null || item.Stage == filter.Stage)
            .Select(item => Item(item, utcNow))
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformCollectionCaseItem>(items.Skip(page.Skip).Take(page.PageSize).ToArray(), page, items.Length));
    }

    public Task<PlatformCollectionCaseItem?> GetCaseAsync(Guid caseId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult(Cases.FirstOrDefault(item => item.Id == caseId) is { } found ? Item(found, utcNow) : null);
    }

    public Task<IReadOnlyList<PlatformCollectionEventItem>> ListEventsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformCollectionEventItem>>(Events
            .Where(item => item.PlatformCollectionCaseId == caseId)
            .OrderByDescending(item => item.CreatedUtc)
            .Select(item => new PlatformCollectionEventItem(item.CreatedUtc, item.EventCode, item.Arguments, item.Note, null))
            .ToArray());
    }

    public Task<IReadOnlyList<PlatformOverdueInvoiceItem>> ListOverdueInvoicesAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformOverdueInvoiceItem>>(Overdue(platformTenantId, utcNow)
            .Select(item => new PlatformOverdueInvoiceItem(item.Id, item.Number, item.DueUtc, item.Balance, item.CurrencyCode, item.PendingCredit is not null))
            .ToArray());
    }

    public Task<PlatformCollectionsDashboard> GetDashboardAsync(DateTimeOffset utcNow, int longBlockDays, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PlatformCollectionsDashboard(
            Cases.Where(item => item.IsOpen).GroupBy(item => item.Stage).ToDictionary(group => group.Key, group => group.Count()),
            Cases.Count(item => item.IsBlocked),
            0,
            0,
            0,
            Invoices().Where(item => item.IsOverdueAt(utcNow)).GroupBy(item => item.CurrencyCode).Select(group => new PlatformMoneyItem(group.Key, group.Sum(item => item.Balance))).ToArray(),
            Array.Empty<PlatformAgeingItem>(),
            Array.Empty<PlatformMoneyItem>()));
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformCollectionsMail mail, CancellationToken cancellationToken)
    {
        Mails.Add((recipient.Email.Value, mail));
        return Task.CompletedTask;
    }

    private IEnumerable<PlatformInvoice> Overdue(Guid tenantId, DateTimeOffset utcNow)
    {
        return Invoices().Where(item => item.PlatformTenantId == tenantId && item.IsOverdueAt(utcNow) && item.Balance > 0m).OrderBy(item => item.DueUtc);
    }

    private PlatformCollectionCaseItem Item(PlatformCollectionCase item, DateTimeOffset utcNow)
    {
        return new PlatformCollectionCaseItem(
            item.Id, item.PlatformTenantId, "Tenant", item.Status, item.Stage, item.OldestDueUtc, item.PlannedBlockUtc, item.BlockedUtc, item.NoAccessUtc,
            item.ExtendedThrough, item.ExtensionsUsed, item.Held, null,
            Overdue(item.PlatformTenantId, utcNow).GroupBy(invoice => invoice.CurrencyCode).Select(group => new PlatformMoneyItem(group.Key, group.Sum(invoice => invoice.Balance))).ToArray(),
            item.CreatedUtc, item.ClosedUtc, item.CloseReason);
    }
}
