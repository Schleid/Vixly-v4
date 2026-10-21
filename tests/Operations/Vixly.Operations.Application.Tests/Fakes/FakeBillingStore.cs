using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeBillingStore.cs
//
// PURPOSE:
// An in-memory store of invoices and credit notes for use case tests (Slice 5F).
//
// RESPONSIBILITIES:
// - Implement IOperationsBillingRepository over lists
//
// IMPORTANT NOTES:
// - Not tenant-filtered: one company per test
// =========================================================
internal sealed class FakeBillingStore : IOperationsBillingRepository
{
    public List<OperationsInvoice> Invoices { get; } = new();

    public List<OperationsCreditNote> CreditNotes { get; } = new();

    public void Add(OperationsInvoice invoice) => Invoices.Add(invoice);

    public void Add(OperationsCreditNote creditNote) => CreditNotes.Add(creditNote);

    public Task<OperationsInvoice?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) => Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.Id == invoiceId));

    public Task<OperationsInvoice?> FindShipmentInvoiceAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.ShipmentId == shipmentId && invoice.Kind == OperationsInvoiceKind.Shipment));

    public Task<OperationsInvoice?> FindOrderInvoiceAsync(Guid supplySaleId, CancellationToken cancellationToken) =>
        Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.SupplySaleId == supplySaleId && invoice.Kind == OperationsInvoiceKind.SupplyOrder));

    public Task<IReadOnlyList<OperationsInvoice>> ListShipmentInvoicesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsInvoice>>(Invoices.Where(invoice => invoice.ShipmentId is { } id && shipmentIds.Contains(id)).ToList());

    public Task<IReadOnlyList<OperationsInvoice>> ListInvoicesAsync(IReadOnlyCollection<Guid> warehouseIds, string? text, OperationsInvoiceFilter filter, int take,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsInvoice>>(Invoices.Where(invoice => warehouseIds.Contains(invoice.WarehouseId)).Take(take).ToList());

    public Task<IReadOnlyList<OperationsInvoice>> ListCustomerInvoicesAsync(Guid customerId, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsInvoice>>(Invoices.Where(invoice => invoice.CustomerId == customerId).Take(take).ToList());

    public Task<bool> AnyInvoiceAsync(CancellationToken cancellationToken) => Task.FromResult(Invoices.Count > 0);

    public Task<OperationsInvoice?> FindByNumberAsync(string number, CancellationToken cancellationToken) =>
        Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.Number == number || invoice.Reference == number));

    public Task<IReadOnlyList<OperationsInvoice>> ListOpenInvoicesAsync(Guid customerId, IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsInvoice>>(Invoices.Where(invoice => invoice.CustomerId == customerId && warehouseIds.Contains(invoice.WarehouseId)
            && (invoice.Owed > 0m || invoice.PendingTotal > 0m)).ToList());

    public Task<OperationsCreditNote?> GetCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken) =>
        Task.FromResult(CreditNotes.FirstOrDefault(note => note.Id == creditNoteId));

    public Task<IReadOnlyList<OperationsCreditNote>> ListCreditNotesAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsCreditNote>>(CreditNotes.Where(note => note.InvoiceId == invoiceId).ToList());

    public Task<IReadOnlyList<OperationsCreditNote>> ListApprovedCreditNotesAsync(IReadOnlyCollection<Guid> invoiceIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsCreditNote>>(CreditNotes.Where(note => invoiceIds.Contains(note.InvoiceId) && note.Status == OperationsCreditNoteStatus.Approved).ToList());

    public Task<IReadOnlyList<OperationsCreditNote>> ListRequestsAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsCreditNote>>(CreditNotes.Where(note => note.Status == OperationsCreditNoteStatus.Requested && warehouseIds.Contains(note.WarehouseId)).ToList());

    public Task<bool> RequestOpenAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        Task.FromResult(CreditNotes.Any(note => note.InvoiceId == invoiceId && note.Status == OperationsCreditNoteStatus.Requested));

    public Task<OperationsCreditNote?> FindByRequestKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        Task.FromResult(CreditNotes.FirstOrDefault(note => note.RequestKey == requestKey));
}
