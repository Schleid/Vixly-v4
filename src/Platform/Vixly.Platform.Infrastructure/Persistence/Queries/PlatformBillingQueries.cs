using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformBillingQueries.cs
//
// PURPOSE:
// Read models for invoices (Slice 4).
//
// RESPONSIBILITIES:
// - Page and filter invoices on the server
// - Sum a tenant's open and overdue balance
//
// IMPORTANT NOTES:
// - No tracking; every list paged (P-1)
// =========================================================
internal sealed class PlatformBillingQueries : IPlatformBillingQueries
{
    private readonly PlatformDbContext _context;

    public PlatformBillingQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformInvoiceListItem>> ListInvoicesAsync(PlatformInvoiceListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(filter.UtcNow);

        var query =
            from invoice in _context.Invoices.AsNoTracking()
            join tenant in _context.Tenants.AsNoTracking() on invoice.PlatformTenantId equals tenant.Id
            select new { invoice, Name = tenant.Company.TradingName ?? tenant.Company.LegalName };

        if (filter.Status is { } status)
        {
            query = query.Where(row => row.invoice.Status == status);
        }

        if (filter.OverdueOnly)
        {
            query = query.Where(row => row.invoice.Status == PlatformInvoiceStatus.Issued && row.invoice.DueUtc < cutoff);
        }

        if (filter.PlatformTenantId is Guid tenantId)
        {
            query = query.Where(row => row.invoice.PlatformTenantId == tenantId);
        }

        if (filter.Search is { } search)
        {
            query = query.Where(row => row.invoice.Number.Contains(search) || row.Name.Contains(search));
        }

        if (filter.IssuedFromUtc is { } from)
        {
            query = query.Where(row => row.invoice.IssuedUtc >= from);
        }

        if (filter.IssuedToUtc is { } to)
        {
            query = query.Where(row => row.invoice.IssuedUtc < to);
        }

        int total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(row => row.invoice.IssuedUtc)
            .ThenByDescending(row => row.invoice.Number)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(row => new
            {
                row.invoice.Id,
                row.invoice.Number,
                row.invoice.PlatformTenantId,
                row.Name,
                row.invoice.Kind,
                row.invoice.Status,
                row.invoice.CurrencyCode,
                row.invoice.Total,
                row.invoice.Balance,
                row.invoice.PaidTotal,
                row.invoice.IssuedUtc,
                row.invoice.DueUtc,
                Pending = row.invoice.Credits.Any(credit => credit.Status == PlatformCreditNoteStatus.Requested)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformInvoiceListItem>(
            rows.Select(row => new PlatformInvoiceListItem(
                row.Id, row.Number, row.PlatformTenantId, row.Name, row.Kind, row.Status, row.CurrencyCode, row.Total, row.Balance, row.PaidTotal,
                row.IssuedUtc, row.DueUtc, row.Status == PlatformInvoiceStatus.Issued && row.DueUtc < cutoff, row.Pending)).ToArray(),
            page,
            total);
    }

    public async Task<PagedResult<PlatformBillingDocumentItem>> ListDocumentsAsync(Guid platformTenantId, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var query =
            from document in _context.BillingDocuments.AsNoTracking()
            join invoice in _context.Invoices.AsNoTracking() on document.PlatformInvoiceId equals invoice.Id
            where invoice.PlatformTenantId == platformTenantId
            select new { document.Kind, document.OwnerId, document.Number, document.PlatformInvoiceId, InvoiceNumber = invoice.Number, document.Language, document.CreatedUtc };

        int total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(row => row.CreatedUtc)
            .ThenByDescending(row => row.Number)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformBillingDocumentItem>(
            rows.Select(row => new PlatformBillingDocumentItem(row.Kind, row.OwnerId, row.Number, row.PlatformInvoiceId, row.InvoiceNumber, row.Language.Value, row.CreatedUtc)).ToArray(),
            page,
            total);
    }

    public async Task<IReadOnlyList<PlatformTenantBillingSummary>> SummaryAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);
        var rows = await _context.Invoices.AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformInvoiceStatus.Issued)
            .GroupBy(item => item.CurrencyCode)
            .Select(group => new
            {
                Currency = group.Key,
                Open = group.Sum(item => item.Balance),
                Overdue = group.Where(item => item.DueUtc < cutoff).Sum(item => (decimal?)item.Balance) ?? 0m,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PlatformTenantBillingSummary(row.Currency, row.Open, row.Overdue, row.Count)).ToArray();
    }
}
