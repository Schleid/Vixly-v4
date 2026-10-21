using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsBillingRepository.cs
//
// PURPOSE:
// Reads and writes invoices and credit notes (Slice 5F step 1).
//
// RESPONSIBILITIES:
// - Add invoices and credit notes
// - Find an invoice by shipment, order or id; list and search them
// - Find the requests to decide
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); lists are bounded by the take given
// =========================================================
public interface IOperationsBillingRepository
{
    void Add(Domain.Billing.OperationsInvoice invoice);

    void Add(OperationsCreditNote creditNote);

    // Tracked.
    Task<Domain.Billing.OperationsInvoice?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    // Read only.
    Task<Domain.Billing.OperationsInvoice?> FindShipmentInvoiceAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Read only.
    Task<Domain.Billing.OperationsInvoice?> FindOrderInvoiceAsync(Guid supplySaleId, CancellationToken cancellationToken);

    // Read only: the invoices of several shipments.
    Task<IReadOnlyList<Domain.Billing.OperationsInvoice>> ListShipmentInvoicesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // Read only, newest first: of these warehouses, matching a text (invoice, shipment or order number, customer code, name or phone) and a state.
    Task<IReadOnlyList<Domain.Billing.OperationsInvoice>> ListInvoicesAsync(IReadOnlyCollection<Guid> warehouseIds, string? text, OperationsInvoiceFilter filter, int take,
        CancellationToken cancellationToken);

    // Read only, newest first.
    Task<IReadOnlyList<Domain.Billing.OperationsInvoice>> ListCustomerInvoicesAsync(Guid customerId, int take, CancellationToken cancellationToken);

    Task<bool> AnyInvoiceAsync(CancellationToken cancellationToken);

    // Slice 5F step 2: read only, an invoice by its number or its shipment's or order's number.
    Task<Domain.Billing.OperationsInvoice?> FindByNumberAsync(string number, CancellationToken cancellationToken);

    // Read only, oldest first: a customer's invoices still to pay or waiting for finance, in these warehouses.
    Task<IReadOnlyList<Domain.Billing.OperationsInvoice>> ListOpenInvoicesAsync(Guid customerId, IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken);

    // Tracked.
    Task<OperationsCreditNote?> GetCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken);

    // Read only, oldest first.
    Task<IReadOnlyList<OperationsCreditNote>> ListCreditNotesAsync(Guid invoiceId, CancellationToken cancellationToken);

    // Read only: the approved credit notes of several invoices.
    Task<IReadOnlyList<OperationsCreditNote>> ListApprovedCreditNotesAsync(IReadOnlyCollection<Guid> invoiceIds, CancellationToken cancellationToken);

    // Read only, oldest first: the requests waiting for a decision in these warehouses.
    Task<IReadOnlyList<OperationsCreditNote>> ListRequestsAsync(IReadOnlyCollection<Guid> warehouseIds, int take, CancellationToken cancellationToken);

    Task<bool> RequestOpenAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<OperationsCreditNote?> FindByRequestKeyAsync(Guid requestKey, CancellationToken cancellationToken);
}
