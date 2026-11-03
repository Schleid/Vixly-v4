using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPayablesRepository.cs
//
// PURPOSE:
// The company's bills, vendors and what they cost each dispatch and shipment (Slice 8).
//
// RESPONSIBILITIES:
// - Add, find and list bills and vendors
// - Find a dispatch or a shipment by its number for a bill's shares
// - Sum the shares of the bills that count (approved or paid) for dispatches and shipments
//
// IMPORTANT NOTES:
// - Tenant-filtered like every Operations read
// =========================================================
public interface IOperationsPayablesRepository
{
    void Add(OperationsVendor vendor);

    Task<IReadOnlyList<OperationsVendor>> ListVendorsAsync(CancellationToken cancellationToken);

    Task<OperationsVendor?> GetVendorAsync(Guid vendorId, CancellationToken cancellationToken);

    Task<bool> VendorNameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    void Add(OperationsBill bill);

    Task<OperationsBill?> GetBillAsync(Guid billId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsBill>> ListBillsAsync(OperationsBillStatus? status, int take, CancellationToken cancellationToken);

    // The same vendor's invoice number entered twice (not voided).
    Task<bool> ReferenceTakenAsync(OperationsBillParty party, Guid partyId, string reference, Guid? exceptId, CancellationToken cancellationToken);

    Task<(Guid Id, string Number)?> FindDispatchAsync(string number, CancellationToken cancellationToken);

    Task<(Guid Id, string Number)?> FindShipmentAsync(string number, CancellationToken cancellationToken);

    // What the counting carrier bills, other than the one given, put on this dispatch, by carrier.
    Task<IReadOnlyList<(Guid CarrierId, decimal Amount)>> CarrierSharesAsync(Guid dispatchId, Guid exceptBillId, CancellationToken cancellationToken);

    // Owed on approved bills, of which overdue (due before today), and bills waiting for approval; over every bill.
    Task<(decimal Owed, decimal Overdue, int ToApprove)> OpenTotalsAsync(DateOnly today, CancellationToken cancellationToken);

    // What the counting bills put directly on these shipments.
    Task<IReadOnlyDictionary<Guid, decimal>> DirectCostsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // The shipments' places on dispatches, with the dispatch (its estimate and what was billed).
    Task<IReadOnlyList<(OperationsDispatchShipment Row, OperationsDispatch Dispatch)>> ListDispatchRowsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // Shipments with an invoice issued in these days (inclusive), newest first.
    Task<IReadOnlyList<Guid>> ListInvoicedShipmentsAsync(DateOnly from, DateOnly to, int take, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> ShipmentNumbersAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);
}
