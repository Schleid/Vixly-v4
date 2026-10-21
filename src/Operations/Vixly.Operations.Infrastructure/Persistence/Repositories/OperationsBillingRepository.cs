using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsBillingRepository.cs
//
// PURPOSE:
// EF Core store of invoices and credit notes (Slice 5F step 1).
//
// RESPONSIBILITIES:
// - Implement IOperationsBillingRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1)
// =========================================================
internal sealed class OperationsBillingRepository : IOperationsBillingRepository
{
    private readonly OperationsDbContext _context;

    public OperationsBillingRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsInvoice invoice) => _context.Invoices.Add(invoice ?? throw new ArgumentNullException(nameof(invoice)));

    public void Add(OperationsCreditNote creditNote) => _context.CreditNotes.Add(creditNote ?? throw new ArgumentNullException(nameof(creditNote)));

    public Task<OperationsInvoice?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        _context.Invoices.FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);

    public Task<OperationsInvoice?> FindShipmentInvoiceAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _context.Invoices.AsNoTracking().FirstOrDefaultAsync(invoice => invoice.ShipmentId == shipmentId && invoice.Kind == OperationsInvoiceKind.Shipment, cancellationToken);

    public Task<OperationsInvoice?> FindOrderInvoiceAsync(Guid supplySaleId, CancellationToken cancellationToken) =>
        _context.Invoices.AsNoTracking().FirstOrDefaultAsync(invoice => invoice.SupplySaleId == supplySaleId && invoice.Kind == OperationsInvoiceKind.SupplyOrder, cancellationToken);

    public async Task<IReadOnlyList<OperationsInvoice>> ListShipmentInvoicesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);

        return shipmentIds.Count == 0
            ? Array.Empty<OperationsInvoice>()
            : await _context.Invoices.AsNoTracking().Where(invoice => invoice.ShipmentId != null && shipmentIds.Contains(invoice.ShipmentId.Value) && invoice.Kind == OperationsInvoiceKind.Shipment)
                .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsInvoice>> ListInvoicesAsync(IReadOnlyCollection<Guid> warehouseIds, string? text, OperationsInvoiceFilter filter, int take,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        IQueryable<OperationsInvoice> query = _context.Invoices.AsNoTracking().Where(invoice => warehouseIds.Contains(invoice.WarehouseId));

        if (!string.IsNullOrWhiteSpace(text))
        {
            string value = text.Trim();
            query = query.Where(invoice => invoice.Number == value || invoice.Reference == value || invoice.Party.CustomerCode == value || invoice.Party.CustomerPhone.Contains(value)
                || invoice.Party.CustomerName.Contains(value) || invoice.Party.CustomerEmail == value);
        }

        query = filter switch
        {
            OperationsInvoiceFilter.Owed => query.Where(invoice => invoice.Status == OperationsInvoiceStatus.Issued && invoice.Total > invoice.CreditedTotal + invoice.PaidTotal),
            OperationsInvoiceFilter.Credited => query.Where(invoice => invoice.CreditedTotal > 0m),
            OperationsInvoiceFilter.Voided => query.Where(invoice => invoice.Status == OperationsInvoiceStatus.Voided),
            _ => query
        };

        return await query.OrderByDescending(invoice => invoice.IssuedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsInvoice>> ListCustomerInvoicesAsync(Guid customerId, int take, CancellationToken cancellationToken) =>
        await _context.Invoices.AsNoTracking().Where(invoice => invoice.CustomerId == customerId).OrderByDescending(invoice => invoice.IssuedUtc).Take(take).ToListAsync(cancellationToken);

    public Task<bool> AnyInvoiceAsync(CancellationToken cancellationToken) => _context.Invoices.AnyAsync(cancellationToken);

    public Task<OperationsCreditNote?> GetCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken) =>
        _context.CreditNotes.FirstOrDefaultAsync(note => note.Id == creditNoteId, cancellationToken);

    public async Task<IReadOnlyList<OperationsCreditNote>> ListCreditNotesAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await _context.CreditNotes.AsNoTracking().Where(note => note.InvoiceId == invoiceId).OrderBy(note => note.RequestedUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsCreditNote>> ListApprovedCreditNotesAsync(IReadOnlyCollection<Guid> invoiceIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoiceIds);

        return invoiceIds.Count == 0
            ? Array.Empty<OperationsCreditNote>()
            : await _context.CreditNotes.AsNoTracking().Where(note => invoiceIds.Contains(note.InvoiceId) && note.Status == OperationsCreditNoteStatus.Approved)
                .OrderBy(note => note.DecidedUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsCreditNote>> ListRequestsAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        return await _context.CreditNotes.AsNoTracking().Where(note => note.Status == OperationsCreditNoteStatus.Requested && warehouseIds.Contains(note.WarehouseId))
            .OrderBy(note => note.RequestedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public Task<bool> RequestOpenAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        _context.CreditNotes.AnyAsync(note => note.InvoiceId == invoiceId && note.Status == OperationsCreditNoteStatus.Requested, cancellationToken);

    public Task<OperationsCreditNote?> FindByRequestKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.CreditNotes.AsNoTracking().FirstOrDefaultAsync(note => note.RequestKey == requestKey, cancellationToken);
}
