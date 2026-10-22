using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsSupplyRepository.cs
//
// PURPOSE:
// Stores the company's shipping supplies with EF Core (Slice 5E).
//
// RESPONSIBILITIES:
// - Implement IOperationsSupplyRepository
//
// IMPORTANT NOTES:
// - Tenant-filtered by the context
// =========================================================
internal sealed class OperationsSupplyRepository : IOperationsSupplyRepository
{
    private readonly OperationsDbContext _context;

    public OperationsSupplyRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsSupply supply) => _context.Supplies.Add(supply ?? throw new ArgumentNullException(nameof(supply)));

    public Task<OperationsSupply?> GetAsync(Guid supplyId, CancellationToken cancellationToken) =>
        _context.Supplies.Include(supply => supply.Barcodes).FirstOrDefaultAsync(supply => supply.Id == supplyId, cancellationToken);

    public async Task<IReadOnlyList<OperationsSupply>> ListAsync(CancellationToken cancellationToken) =>
        await _context.Supplies.AsNoTracking().Include(supply => supply.Barcodes).AsSplitQuery()
            .OrderBy(supply => supply.Category).ThenBy(supply => supply.NameEn).Take(OperationsSupplyLimits.MaxSupplies).ToListAsync(cancellationToken);

    public async Task<OperationsSupply?> FindByScanAsync(string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        Guid? id = await CodeOwnerAsync(code, null, cancellationToken);
        return id is { } found
            ? await _context.Supplies.AsNoTracking().Include(supply => supply.Barcodes).FirstOrDefaultAsync(supply => supply.Id == found, cancellationToken)
            : null;
    }

    public async Task<Guid?> CodeOwnerAsync(string code, Guid? except, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        Guid? byCode = await _context.Supplies.AsNoTracking().Where(supply => supply.Code == code && supply.Id != except).Select(supply => (Guid?)supply.Id).FirstOrDefaultAsync(cancellationToken);
        return byCode ?? await _context.SupplyBarcodes.AsNoTracking().Where(barcode => barcode.Code == code && barcode.SupplyId != except)
            .Select(barcode => (Guid?)barcode.SupplyId).FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(OperationsSupplyStock stock) => _context.SupplyStocks.Add(stock ?? throw new ArgumentNullException(nameof(stock)));

    public void Add(OperationsSupplyMovement movement) => _context.SupplyMovements.Add(movement ?? throw new ArgumentNullException(nameof(movement)));

    public void Add(OperationsSupplyTransfer transfer) => _context.SupplyTransfers.Add(transfer ?? throw new ArgumentNullException(nameof(transfer)));

    public void Add(OperationsSupplyCount count) => _context.SupplyCounts.Add(count ?? throw new ArgumentNullException(nameof(count)));

    // A row added in this unit of work counts too, so two lines of one form for the same supply move one row.
    public async Task<OperationsSupplyStock?> GetStockAsync(Guid supplyId, Guid warehouseId, CancellationToken cancellationToken) =>
        _context.SupplyStocks.Local.FirstOrDefault(stock => stock.SupplyId == supplyId && stock.WarehouseId == warehouseId)
        ?? await _context.SupplyStocks.FirstOrDefaultAsync(stock => stock.SupplyId == supplyId && stock.WarehouseId == warehouseId, cancellationToken);

    public async Task<IReadOnlyList<OperationsSupplyStock>> ListStockAsync(Guid? warehouseId, CancellationToken cancellationToken) =>
        await _context.SupplyStocks.AsNoTracking().Where(stock => warehouseId == null || stock.WarehouseId == warehouseId)
            .Take(OperationsSupplyLimits.MaxSupplies * 20).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsSupplyMovement>> ListMovementsAsync(Guid supplyId, Guid warehouseId, int take, CancellationToken cancellationToken) =>
        await _context.SupplyMovements.AsNoTracking().Where(movement => movement.SupplyId == supplyId && movement.WarehouseId == warehouseId)
            .OrderByDescending(movement => movement.CreatedUtc).Take(Math.Clamp(take, 1, 500)).ToListAsync(cancellationToken);

    public Task<bool> LinkUsedAsync(Guid linkId, CancellationToken cancellationToken) =>
        _context.SupplyMovements.AnyAsync(movement => movement.LinkId == linkId, cancellationToken);

    public Task<OperationsSupplyTransfer?> GetTransferAsync(Guid transferId, CancellationToken cancellationToken) =>
        _context.SupplyTransfers.FirstOrDefaultAsync(transfer => transfer.Id == transferId, cancellationToken);

    public async Task<IReadOnlyList<OperationsSupplyTransfer>> ListTransfersInTransitAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        await _context.SupplyTransfers.AsNoTracking().Where(transfer => transfer.ReceivedUtc == null && (transfer.FromWarehouseId == warehouseId || transfer.ToWarehouseId == warehouseId))
            .OrderBy(transfer => transfer.CreatedUtc).Take(200).ToListAsync(cancellationToken);

    public Task<OperationsSupplyCount?> GetCountAsync(Guid countId, CancellationToken cancellationToken) =>
        _context.SupplyCounts.FirstOrDefaultAsync(count => count.Id == countId, cancellationToken);

    public void Add(OperationsSupplySale sale) => _context.SupplySales.Add(sale ?? throw new ArgumentNullException(nameof(sale)));

    public Task<OperationsSupplySale?> GetSaleAsync(Guid saleId, CancellationToken cancellationToken) =>
        _context.SupplySales.FirstOrDefaultAsync(sale => sale.Id == saleId, cancellationToken);

    public Task<OperationsSupplySale?> FindShipmentSaleAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _context.SupplySales.FirstOrDefaultAsync(sale => sale.ShipmentId == shipmentId && sale.Status != OperationsSupplySaleStatus.Cancelled, cancellationToken);

    public async Task<IReadOnlyList<OperationsSupplySale>> ListShipmentSalesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);
        return shipmentIds.Count == 0
            ? Array.Empty<OperationsSupplySale>()
            : await _context.SupplySales.AsNoTracking().Where(sale => sale.ShipmentId != null && shipmentIds.Contains(sale.ShipmentId.Value) && sale.Status != OperationsSupplySaleStatus.Cancelled)
                .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsSupplySale>> ListCustomerOrdersAsync(Guid customerId, int take, CancellationToken cancellationToken) =>
        await _context.SupplySales.AsNoTracking().Where(sale => sale.CustomerId == customerId && sale.ShipmentId == null)
            .OrderByDescending(sale => sale.CreatedUtc).Take(Math.Clamp(take, 1, 100)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsSupply>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        string term = text.Trim();
        return await _context.Supplies.AsNoTracking().Include(supply => supply.Barcodes)
            .Where(supply => supply.IsActive && (supply.Code.Contains(term) || supply.NameEn.Contains(term) || (supply.NameFr != null && supply.NameFr.Contains(term))
                || (supply.NameEs != null && supply.NameEs.Contains(term))))
            .OrderBy(supply => supply.Code).Take(Math.Clamp(take, 1, 20)).ToListAsync(cancellationToken);
    }

    public Task<OperationsSupplyCount?> FindOpenCountAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        _context.SupplyCounts.AsNoTracking().FirstOrDefaultAsync(count => count.WarehouseId == warehouseId && count.ClosedUtc == null, cancellationToken);
}
