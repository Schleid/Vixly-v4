using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSupplyRepository.cs
//
// PURPOSE:
// Stores the company's shipping supplies (Slice 5E).
//
// RESPONSIBILITIES:
// - Add, find and list supplies in the current tenant, with their barcodes
// - Find a supply by a scan (barcode or item code)
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); lists are bounded by OperationsSupplyLimits.MaxSupplies
// =========================================================
public interface IOperationsSupplyRepository
{
    void Add(OperationsSupply supply);

    // Tracked, with its barcodes.
    Task<OperationsSupply?> GetAsync(Guid supplyId, CancellationToken cancellationToken);

    // Read only, with their barcodes, by category then name.
    Task<IReadOnlyList<OperationsSupply>> ListAsync(CancellationToken cancellationToken);

    // A barcode or an item code exactly as a scanner sends it (in capitals); read only.
    Task<OperationsSupply?> FindByScanAsync(string code, CancellationToken cancellationToken);

    // The supply already using this code as its item code or as a barcode, other than the one given.
    Task<Guid?> CodeOwnerAsync(string code, Guid? except, CancellationToken cancellationToken);

    // Slice 5E step 2: the stock.
    void Add(OperationsSupplyStock stock);

    void Add(OperationsSupplyMovement movement);

    void Add(OperationsSupplyTransfer transfer);

    void Add(OperationsSupplyCount count);

    // Tracked; null when nothing moved there yet.
    Task<OperationsSupplyStock?> GetStockAsync(Guid supplyId, Guid warehouseId, CancellationToken cancellationToken);

    // Read only: the stock rows of one warehouse, or of all when none is given.
    Task<IReadOnlyList<OperationsSupplyStock>> ListStockAsync(Guid? warehouseId, CancellationToken cancellationToken);

    // Read only, newest first.
    Task<IReadOnlyList<OperationsSupplyMovement>> ListMovementsAsync(Guid supplyId, Guid warehouseId, int take, CancellationToken cancellationToken);

    // A form already applied (its key, ADR-0019).
    Task<bool> LinkUsedAsync(Guid linkId, CancellationToken cancellationToken);

    Task<OperationsSupplyTransfer?> GetTransferAsync(Guid transferId, CancellationToken cancellationToken);

    // Read only: transfers still in transit from or to a warehouse.
    Task<IReadOnlyList<OperationsSupplyTransfer>> ListTransfersInTransitAsync(Guid warehouseId, CancellationToken cancellationToken);

    Task<OperationsSupplyCount?> GetCountAsync(Guid countId, CancellationToken cancellationToken);

    // Read only: the open count of a warehouse.
    Task<OperationsSupplyCount?> FindOpenCountAsync(Guid warehouseId, CancellationToken cancellationToken);

    // Slice 5E step 3: supplies sold at the counter.
    void Add(OperationsSupplySale sale);

    Task<OperationsSupplySale?> GetSaleAsync(Guid saleId, CancellationToken cancellationToken);

    // Tracked: the shipment's supplies that are not cancelled.
    Task<OperationsSupplySale?> FindShipmentSaleAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Read only: the supplies of several shipments, not cancelled.
    Task<IReadOnlyList<OperationsSupplySale>> ListShipmentSalesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // Read only: a customer's orders of supplies on their own, newest first.
    Task<IReadOnlyList<OperationsSupplySale>> ListCustomerOrdersAsync(Guid customerId, int take, CancellationToken cancellationToken);

    // Read only: the supplies matching a text (code or name), at most a few, for the clerk to choose.
    Task<IReadOnlyList<OperationsSupply>> SearchAsync(string text, int take, CancellationToken cancellationToken);
}
