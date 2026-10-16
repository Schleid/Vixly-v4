using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformInvoiceRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformInvoiceRepository and IPlatformInvoiceHistory.
//
// RESPONSIBILITIES:
// - Load and add invoices and their history
//
// IMPORTANT NOTES:
// - Owned lines, payments and credit notes load with the invoice
// =========================================================
internal sealed class PlatformInvoiceRepository : IPlatformInvoiceRepository, IPlatformInvoiceHistory
{
    private readonly PlatformDbContext _context;

    public PlatformInvoiceRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return _context.Invoices.FirstOrDefaultAsync(item => item.Id == invoiceId, cancellationToken);
    }

    public Task<bool> SourceBilledAsync(string sourceKey, CancellationToken cancellationToken)
    {
        return _context.Invoices.AsNoTracking().AnyAsync(item => item.SourceKey == sourceKey, cancellationToken);
    }

    public async Task<Guid?> FindOpenFirstPeriodAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return await _context.Invoices.AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId && item.Kind == PlatformInvoiceKind.FirstPeriod && item.Status == PlatformInvoiceStatus.Issued)
            .OrderByDescending(item => item.IssuedUtc)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListOverdueUnmarkedAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);

        return await _context.Invoices.AsNoTracking()
            .Where(item => item.Status == PlatformInvoiceStatus.Issued && item.DueUtc < cutoff && item.PastDueUtc == null)
            .OrderBy(item => item.DueUtc)
            .Select(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformInvoice>> ListOverdueForTenantAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);

        return await _context.Invoices
            .Where(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformInvoiceStatus.Issued && item.Balance > 0m && item.DueUtc < cutoff)
            .OrderBy(item => item.DueUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasOverdueAsync(Guid platformSubscriptionId, Guid exceptInvoiceId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = PlatformInvoice.OverdueCutoff(utcNow);

        return _context.Invoices.AsNoTracking().AnyAsync(
            item => item.PlatformSubscriptionId == platformSubscriptionId && item.Id != exceptInvoiceId
                && item.Status == PlatformInvoiceStatus.Issued && item.DueUtc < cutoff,
            cancellationToken);
    }

    public void Add(PlatformInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _context.Invoices.Add(invoice);
    }

    public void AddEvents(IEnumerable<PlatformInvoiceEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        _context.InvoiceEvents.AddRange(events);
    }

    public async Task<IReadOnlyList<PlatformInvoiceEvent>> ListAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return await _context.InvoiceEvents.AsNoTracking()
            .Where(item => item.PlatformInvoiceId == invoiceId)
            .OrderByDescending(item => item.CreatedUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}
