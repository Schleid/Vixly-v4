using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformPaymentsQueries.cs
//
// PURPOSE:
// Read-only card-payment queries: the transactions list and the clock's work lists.
//
// RESPONSIBILITIES:
// - Page card transactions with tenant and invoice names
// - Find invoices due for automatic charging, stale transactions, unsent refunds
//
// IMPORTANT NOTES:
// - No-tracking projections; every list is bounded (P-1)
// =========================================================
internal sealed class PlatformPaymentsQueries : IPlatformPaymentsQueries
{
    private readonly PlatformDbContext _context;

    public PlatformPaymentsQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformCardTransactionItem>> ListTransactionsAsync(PlatformCardTransactionFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        var query =
            from transaction in _context.CardTransactions.AsNoTracking()
            join invoice in _context.Invoices.AsNoTracking() on transaction.PlatformInvoiceId equals invoice.Id
            join tenant in _context.Tenants.AsNoTracking() on transaction.PlatformTenantId equals tenant.Id
            select new { transaction, invoice.Number, Name = tenant.Company.TradingName ?? tenant.Company.LegalName };

        if (filter.Status is { } status)
        {
            query = query.Where(row => row.transaction.Status == status);
        }

        if (filter.Origin is { } origin)
        {
            query = query.Where(row => row.transaction.Origin == origin);
        }

        if (filter.PlatformTenantId is Guid tenantId)
        {
            query = query.Where(row => row.transaction.PlatformTenantId == tenantId);
        }

        if (filter.PlatformInvoiceId is Guid invoiceId)
        {
            query = query.Where(row => row.transaction.PlatformInvoiceId == invoiceId);
        }

        if (filter.DisputedOnly)
        {
            query = query.Where(row => row.transaction.Dispute != null);
        }

        int total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(row => row.transaction.CreatedUtc)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);
        Guid[] staff = rows.Where(row => row.transaction.CreatedByUserId != null).Select(row => row.transaction.CreatedByUserId!.Value).Distinct().ToArray();
        Dictionary<Guid, string> names = staff.Length == 0
            ? new Dictionary<Guid, string>()
            : await _context.Users.AsNoTracking().Where(user => staff.Contains(user.Id)).ToDictionaryAsync(user => user.Id, user => user.FirstName + " " + user.LastName, cancellationToken);

        return new PagedResult<PlatformCardTransactionItem>(
            rows.Select(row => new PlatformCardTransactionItem(
                row.transaction.Id,
                row.transaction.PlatformTenantId,
                row.Name,
                row.transaction.PlatformInvoiceId,
                row.Number,
                row.transaction.Kind,
                row.transaction.Origin,
                row.transaction.Status,
                row.transaction.Amount,
                row.transaction.CurrencyCode,
                row.transaction.FailureCode,
                row.transaction.Dispute,
                row.transaction.ProviderRefundId ?? row.transaction.ProviderPaymentId,
                row.transaction.CreatedUtc,
                row.transaction.CompletedUtc,
                row.transaction.CreatedByUserId is Guid id && names.TryGetValue(id, out string? name) ? name : null)).ToArray(),
            page,
            total);
    }

    public async Task<IReadOnlyList<Guid>> ListAutoChargeInvoicesAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken)
    {
        return await (
            from invoice in _context.Invoices.AsNoTracking()
            join profile in _context.TenantCardProfiles.AsNoTracking() on invoice.PlatformTenantId equals profile.PlatformTenantId
            where profile.AutoCharge
                && invoice.Status == PlatformInvoiceStatus.Issued
                && invoice.Balance > 0m
                && invoice.DueUtc <= utcNow
                && !_context.CardTransactions.Any(item => item.PlatformInvoiceId == invoice.Id && item.Origin == PlatformCardTransactionOrigin.Automatic)
            orderby invoice.DueUtc
            select invoice.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListStaleTransactionsAsync(DateTimeOffset createdBeforeUtc, int take, CancellationToken cancellationToken)
    {
        return await _context.CardTransactions.AsNoTracking()
            .Where(item => (item.Status == PlatformCardTransactionStatus.Created || item.Status == PlatformCardTransactionStatus.Pending || item.Status == PlatformCardTransactionStatus.RequiresAction)
                && (item.LastModifiedUtc ?? item.CreatedUtc) < createdBeforeUtc)
            .OrderBy(item => item.LastModifiedUtc ?? item.CreatedUtc)
            .Select(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListUnsentRefundsAsync(int take, CancellationToken cancellationToken)
    {
        return await _context.CardTransactions.AsNoTracking()
            .Where(item => item.Kind == PlatformCardTransactionKind.Refund && item.Status == PlatformCardTransactionStatus.Created)
            .OrderBy(item => item.CreatedUtc)
            .Select(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
