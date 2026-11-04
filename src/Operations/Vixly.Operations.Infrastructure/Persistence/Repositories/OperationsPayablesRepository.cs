using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsPayablesRepository.cs
//
// PURPOSE:
// EF Core store of the company's bills and vendors, and the cost queries of the profit per shipment (Slice 8).
//
// RESPONSIBILITIES:
// - Implement IOperationsPayablesRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the model (D-F1); the sums are worked out in SQL over the bills that count (approved or paid)
// =========================================================
internal sealed class OperationsPayablesRepository : IOperationsPayablesRepository
{
    private static readonly OperationsBillStatus[] Counting = { OperationsBillStatus.Approved, OperationsBillStatus.Paid };

    private readonly OperationsDbContext _context;

    public OperationsPayablesRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsVendor vendor) => _context.Vendors.Add(vendor ?? throw new ArgumentNullException(nameof(vendor)));

    public async Task<IReadOnlyList<OperationsVendor>> ListVendorsAsync(CancellationToken cancellationToken) =>
        await _context.Vendors.OrderBy(vendor => vendor.Name).Take(500).ToListAsync(cancellationToken);

    public Task<OperationsVendor?> GetVendorAsync(Guid vendorId, CancellationToken cancellationToken) =>
        _context.Vendors.FirstOrDefaultAsync(vendor => vendor.Id == vendorId, cancellationToken);

    public Task<bool> VendorNameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        _context.Vendors.AnyAsync(vendor => vendor.Name == name && vendor.Id != exceptId, cancellationToken);

    public void Add(OperationsBill bill) => _context.Bills.Add(bill ?? throw new ArgumentNullException(nameof(bill)));

    public Task<OperationsBill?> GetBillAsync(Guid billId, CancellationToken cancellationToken) =>
        _context.Bills.FirstOrDefaultAsync(bill => bill.Id == billId, cancellationToken);

    public async Task<IReadOnlyList<OperationsBill>> ListBillsAsync(OperationsBillStatus? status, int take, CancellationToken cancellationToken) =>
        await _context.Bills.AsNoTracking().Where(bill => status == null || bill.Status == status)
            .OrderByDescending(bill => bill.BilledOn).ThenByDescending(bill => bill.CreatedUtc).Take(Math.Clamp(take, 1, 500)).ToListAsync(cancellationToken);

    public Task<bool> ReferenceTakenAsync(OperationsBillParty party, Guid partyId, string reference, Guid? exceptId, CancellationToken cancellationToken) =>
        _context.Bills.AnyAsync(bill => bill.Party == party && (bill.CarrierId == partyId || bill.PartnerWarehouseId == partyId || bill.VendorId == partyId)
            && bill.Reference == reference && bill.Status != OperationsBillStatus.Voided && bill.Id != exceptId, cancellationToken);

    public async Task<(Guid Id, string Number)?> FindDispatchAsync(string number, CancellationToken cancellationToken) =>
        await _context.Dispatches.AsNoTracking().Where(dispatch => dispatch.Number == number).Select(dispatch => new { dispatch.Id, dispatch.Number })
            .FirstOrDefaultAsync(cancellationToken) is { } found ? (found.Id, found.Number) : null;

    public async Task<(Guid Id, string Number)?> FindShipmentAsync(string number, CancellationToken cancellationToken) =>
        await _context.Shipments.AsNoTracking().Where(shipment => shipment.Number == number).Select(shipment => new { shipment.Id, shipment.Number })
            .FirstOrDefaultAsync(cancellationToken) is { } found ? (found.Id, found.Number!) : null;

    public async Task<IReadOnlyList<(Guid CarrierId, decimal Amount)>> CarrierSharesAsync(Guid dispatchId, Guid exceptBillId, CancellationToken cancellationToken)
    {
        var rows = await _context.Bills.Where(bill => Counting.Contains(bill.Status) && bill.Id != exceptBillId && bill.Party == OperationsBillParty.Carrier && bill.CarrierId != null)
            .SelectMany(bill => bill.Allocations.Where(allocation => allocation.DispatchId == dispatchId).Select(allocation => new { CarrierId = bill.CarrierId!.Value, allocation.Amount }))
            .ToListAsync(cancellationToken);
        return rows.Select(row => (row.CarrierId, row.Amount)).ToList();
    }

    public async Task<(decimal Owed, decimal Overdue, int ToApprove)> OpenTotalsAsync(DateOnly today, CancellationToken cancellationToken)
    {
        decimal owed = await _context.Bills.Where(bill => bill.Status == OperationsBillStatus.Approved).SumAsync(bill => bill.Amount - bill.PaidTotal, cancellationToken);
        decimal overdue = await _context.Bills.Where(bill => bill.Status == OperationsBillStatus.Approved && bill.DueOn < today)
            .SumAsync(bill => bill.Amount - bill.PaidTotal, cancellationToken);
        int toApprove = await _context.Bills.CountAsync(bill => bill.Status == OperationsBillStatus.Entered, cancellationToken);
        return (owed, overdue, toApprove);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> DirectCostsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        var rows = await _context.Bills.Where(bill => Counting.Contains(bill.Status)).SelectMany(bill => bill.Allocations)
            .Where(allocation => allocation.ShipmentId != null && shipmentIds.Contains(allocation.ShipmentId.Value))
            .Select(allocation => new { allocation.ShipmentId, allocation.Amount }).ToListAsync(cancellationToken);
        return rows.GroupBy(row => row.ShipmentId!.Value).ToDictionary(group => group.Key, group => group.Sum(row => row.Amount));
    }

    public async Task<IReadOnlyList<(OperationsDispatchShipment Row, OperationsDispatch Dispatch)>> ListDispatchRowsAsync(IReadOnlyCollection<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        var rows = await (from row in _context.DispatchShipments.AsNoTracking()
                          join dispatch in _context.Dispatches.AsNoTracking() on row.DispatchId equals dispatch.Id
                          where shipmentIds.Contains(row.ShipmentId) && dispatch.Status != OperationsDispatchStatus.Cancelled
                          select new { row, dispatch }).ToListAsync(cancellationToken);
        return rows.Select(item => (item.row, item.dispatch)).ToList();
    }

    // Review: grouped so the newest are kept, voided invoices left out.
    public async Task<IReadOnlyList<Guid>> ListInvoicedShipmentsAsync(DateOnly from, DateOnly to, int take, CancellationToken cancellationToken) =>
        await _context.Invoices.AsNoTracking()
            .Where(invoice => invoice.ShipmentId != null && invoice.Kind == OperationsInvoiceKind.Shipment && invoice.Status != OperationsInvoiceStatus.Voided
                && invoice.IssuedOn >= from && invoice.IssuedOn <= to)
            .GroupBy(invoice => invoice.ShipmentId!.Value).Select(group => new { Id = group.Key, Issued = group.Max(invoice => invoice.IssuedOn) })
            .OrderByDescending(row => row.Issued).ThenBy(row => row.Id).Take(Math.Clamp(take, 1, 2000)).Select(row => row.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> ShipmentNumbersAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken) =>
        await _context.Shipments.AsNoTracking().Where(shipment => shipmentIds.Contains(shipment.Id))
            .ToDictionaryAsync(shipment => shipment.Id, shipment => shipment.Number ?? string.Empty, cancellationToken);
}
