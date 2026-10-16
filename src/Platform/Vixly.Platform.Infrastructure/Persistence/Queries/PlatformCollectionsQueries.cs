using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformCollectionsQueries.cs
//
// PURPOSE:
// Read models for the collections screens (D-C13, D-C14).
//
// RESPONSIBILITIES:
// - Page and filter cases with their overdue amounts
// - Compute the dashboard with grouped queries
// - Read history with actor names and a tenant's overdue invoices
//
// IMPORTANT NOTES:
// - No tracking; amounts per currency, never converted
// =========================================================
internal sealed class PlatformCollectionsQueries : IPlatformCollectionsQueries
{
    private readonly PlatformDbContext _context;

    public PlatformCollectionsQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformCollectionCaseItem>> ListCasesAsync(PlatformCollectionCaseFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);
        var query =
            from collectionCase in _context.CollectionCases.AsNoTracking()
            join tenant in _context.Tenants.AsNoTracking() on collectionCase.PlatformTenantId equals tenant.Id
            select new { collectionCase, Name = tenant.Company.TradingName ?? tenant.Company.LegalName };

        if (filter.Status is { } status)
        {
            query = query.Where(row => row.collectionCase.Status == status);
        }

        if (filter.Stage is { } stage)
        {
            query = query.Where(row => row.collectionCase.Stage == stage);
        }

        if (filter.PlatformTenantId is Guid tenantId)
        {
            query = query.Where(row => row.collectionCase.PlatformTenantId == tenantId);
        }

        if (filter.InExtension is { } inExtension)
        {
            query = inExtension
                ? query.Where(row => row.collectionCase.ExtensionUntilUtc != null && row.collectionCase.ExtensionUntilUtc > utcNow)
                : query.Where(row => row.collectionCase.ExtensionUntilUtc == null || row.collectionCase.ExtensionUntilUtc <= utcNow);
        }

        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            string currency = filter.CurrencyCode;
            query = query.Where(row => _context.Invoices.Any(invoice => invoice.PlatformTenantId == row.collectionCase.PlatformTenantId
                && invoice.Status == PlatformInvoiceStatus.Issued && invoice.DueUtc < cutoff && invoice.CurrencyCode == currency));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search;
            query = query.Where(row => row.Name.Contains(search));
        }

        int total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(row => row.collectionCase.Status)
            .ThenByDescending(row => row.collectionCase.Stage)
            .ThenBy(row => row.collectionCase.OldestDueUtc)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, IReadOnlyList<PlatformMoneyItem>> overdue = await OverdueAsync(rows.Select(row => row.collectionCase.PlatformTenantId).Distinct().ToArray(), cutoff, cancellationToken);
        Dictionary<Guid, PlatformSubscriptionStatus> statuses = await StatusesAsync(rows.Where(row => row.collectionCase.PlatformSubscriptionId != null).Select(row => row.collectionCase.PlatformSubscriptionId!.Value).Distinct().ToArray(), cancellationToken);

        return new PagedResult<PlatformCollectionCaseItem>(
            rows.Select(row => Item(row.collectionCase, row.Name, overdue, statuses)).ToArray(),
            page,
            total);
    }

    public async Task<PlatformCollectionCaseItem?> GetCaseAsync(Guid caseId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var row = await (
            from collectionCase in _context.CollectionCases.AsNoTracking()
            join tenant in _context.Tenants.AsNoTracking() on collectionCase.PlatformTenantId equals tenant.Id
            where collectionCase.Id == caseId
            select new { collectionCase, Name = tenant.Company.TradingName ?? tenant.Company.LegalName }).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        Dictionary<Guid, IReadOnlyList<PlatformMoneyItem>> overdue = await OverdueAsync(new[] { row.collectionCase.PlatformTenantId }, PlatformInvoice.OverdueCutoff(utcNow), cancellationToken);
        Dictionary<Guid, PlatformSubscriptionStatus> statuses = row.collectionCase.PlatformSubscriptionId is Guid subscriptionId
            ? await StatusesAsync(new[] { subscriptionId }, cancellationToken)
            : new Dictionary<Guid, PlatformSubscriptionStatus>();

        return Item(row.collectionCase, row.Name, overdue, statuses);
    }

    public async Task<IReadOnlyList<PlatformCollectionEventItem>> ListEventsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var rows = await (
            from item in _context.CollectionCaseEvents.AsNoTracking()
            where item.PlatformCollectionCaseId == caseId
            join user in _context.Users.AsNoTracking() on item.ActorUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            orderby item.CreatedUtc descending
            select new { item.CreatedUtc, item.EventCode, item.Arguments, item.Note, Actor = user == null ? null : user.FirstName + " " + user.LastName })
            .Take(500)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PlatformCollectionEventItem(row.CreatedUtc, row.EventCode, row.Arguments, row.Note, row.Actor)).ToArray();
    }

    public async Task<IReadOnlyList<PlatformOverdueInvoiceItem>> ListOverdueInvoicesAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);
        List<PlatformInvoice> invoices = await _context.Invoices.AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformInvoiceStatus.Issued && item.Balance > 0m && item.DueUtc < cutoff)
            .OrderBy(item => item.DueUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return invoices.Select(item => new PlatformOverdueInvoiceItem(item.Id, item.Number, item.DueUtc, item.Balance, item.CurrencyCode, item.PendingCredit is not null)).ToArray();
    }

    public async Task<PlatformCollectionsDashboard> GetDashboardAsync(DateTimeOffset utcNow, int longBlockDays, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);
        IQueryable<PlatformCollectionCase> open = _context.CollectionCases.AsNoTracking().Where(item => item.Status == PlatformCollectionCaseStatus.Open);

        Dictionary<PlatformCollectionStage, int> byStage = await open.GroupBy(item => item.Stage)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Key, row => row.Count, cancellationToken);
        DateTimeOffset soon = utcNow.AddDays(7);
        int ending = await open.CountAsync(item => item.ExtensionUntilUtc != null && item.ExtensionUntilUtc > utcNow && item.ExtensionUntilUtc <= soon, cancellationToken);
        DateTimeOffset longSince = utcNow.AddDays(-longBlockDays);
        int blocked = await _context.Subscriptions.AsNoTracking().CountAsync(item => item.Status == PlatformSubscriptionStatus.BlockedForNonPayment, cancellationToken);
        int longBlocks = await _context.Subscriptions.AsNoTracking().CountAsync(item => item.Status == PlatformSubscriptionStatus.BlockedForNonPayment && item.BlockedUtc <= longSince, cancellationToken);
        DateTimeOffset weekAgo = utcNow.AddDays(-7);
        int failedRetries = await _context.CardTransactions.AsNoTracking().CountAsync(item => item.Origin == PlatformCardTransactionOrigin.Retry && item.Status == PlatformCardTransactionStatus.Failed && item.CreatedUtc >= weekAgo, cancellationToken);

        var overdueRows = await _context.Invoices.AsNoTracking()
            .Where(item => item.Status == PlatformInvoiceStatus.Issued && item.Balance > 0m && item.DueUtc < cutoff)
            .Select(item => new { item.CurrencyCode, item.Balance, item.DueUtc })
            .ToListAsync(cancellationToken);

        PlatformMoneyItem[] overdue = overdueRows.GroupBy(row => row.CurrencyCode)
            .Select(group => new PlatformMoneyItem(group.Key, group.Sum(row => row.Balance)))
            .OrderBy(item => item.CurrencyCode)
            .ToArray();
        PlatformAgeingItem[] ageing = overdueRows
            .GroupBy(row => (Bucket: Bucket(PlatformCollectionsLimits.DayOf(row.DueUtc, utcNow)), row.CurrencyCode))
            .Select(group => new PlatformAgeingItem(group.Key.Bucket, group.Key.CurrencyCode, group.Sum(row => row.Balance)))
            .OrderBy(item => item.Bucket).ThenBy(item => item.CurrencyCode)
            .ToArray();

        // Recovered: payments in the last 30 days on invoices that were overdue when paid.
        DateTimeOffset monthAgo = utcNow.AddDays(-30);
        List<PlatformInvoice> paid = await _context.Invoices.AsNoTracking()
            .Where(item => item.PastDueUtc != null || item.Status == PlatformInvoiceStatus.Paid)
            .Where(item => item.LastModifiedUtc >= monthAgo)
            .ToListAsync(cancellationToken);
        PlatformMoneyItem[] recovered = paid
            .SelectMany(invoice => invoice.Payments.Where(payment => payment.Counts && payment.ReceivedUtc >= monthAgo && payment.ReceivedUtc.UtcDateTime.Date > invoice.DueUtc.UtcDateTime.Date)
                .Select(payment => new { invoice.CurrencyCode, payment.Amount }))
            .GroupBy(row => row.CurrencyCode)
            .Select(group => new PlatformMoneyItem(group.Key, group.Sum(row => row.Amount)))
            .OrderBy(item => item.CurrencyCode)
            .ToArray();

        return new PlatformCollectionsDashboard(byStage, blocked, ending, longBlocks, failedRetries, overdue, ageing, recovered);
    }

    private static int Bucket(int day) => day <= 30 ? 1 : day <= 60 ? 2 : day <= 90 ? 3 : 4;

    private async Task<Dictionary<Guid, IReadOnlyList<PlatformMoneyItem>>> OverdueAsync(Guid[] tenantIds, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        if (tenantIds.Length == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<PlatformMoneyItem>>();
        }

        var rows = await _context.Invoices.AsNoTracking()
            .Where(item => tenantIds.Contains(item.PlatformTenantId) && item.Status == PlatformInvoiceStatus.Issued && item.Balance > 0m && item.DueUtc < cutoff)
            .GroupBy(item => new { item.PlatformTenantId, item.CurrencyCode })
            .Select(group => new { group.Key.PlatformTenantId, group.Key.CurrencyCode, Amount = group.Sum(item => item.Balance) })
            .ToListAsync(cancellationToken);

        return rows.GroupBy(row => row.PlatformTenantId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PlatformMoneyItem>)group.Select(row => new PlatformMoneyItem(row.CurrencyCode, row.Amount)).OrderBy(item => item.CurrencyCode).ToArray());
    }

    private async Task<Dictionary<Guid, PlatformSubscriptionStatus>> StatusesAsync(Guid[] subscriptionIds, CancellationToken cancellationToken)
    {
        return subscriptionIds.Length == 0
            ? new Dictionary<Guid, PlatformSubscriptionStatus>()
            : await _context.Subscriptions.AsNoTracking().Where(item => subscriptionIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Status, cancellationToken);
    }

    private static PlatformCollectionCaseItem Item(
        PlatformCollectionCase collectionCase,
        string name,
        Dictionary<Guid, IReadOnlyList<PlatformMoneyItem>> overdue,
        Dictionary<Guid, PlatformSubscriptionStatus> statuses)
    {
        return new PlatformCollectionCaseItem(
            collectionCase.Id,
            collectionCase.PlatformTenantId,
            name,
            collectionCase.Status,
            collectionCase.Stage,
            collectionCase.OldestDueUtc,
            collectionCase.PlannedBlockUtc,
            collectionCase.BlockedUtc,
            collectionCase.NoAccessUtc,
            collectionCase.ExtendedThrough,
            collectionCase.ExtensionsUsed,
            collectionCase.Held,
            collectionCase.PlatformSubscriptionId is Guid id && statuses.TryGetValue(id, out PlatformSubscriptionStatus status) ? status : null,
            collectionCase.IsOpen && overdue.TryGetValue(collectionCase.PlatformTenantId, out IReadOnlyList<PlatformMoneyItem>? amounts) ? amounts : Array.Empty<PlatformMoneyItem>(),
            collectionCase.CreatedUtc,
            collectionCase.ClosedUtc,
            collectionCase.CloseReason);
    }
}
