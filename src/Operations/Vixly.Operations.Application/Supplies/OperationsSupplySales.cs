using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplySales.cs
//
// PURPOSE:
// Supplies sold at the counter (Slice 5E, S-11 to S-15): their price, the stock rule, confirming and cancelling.
//
// RESPONSIBILITIES:
// - Price a supply for a warehouse and a quantity from its price today, and keep that price version on the line
// - Apply the company's rule when the stock runs out (S-9)
// - Confirm a sale (stock sold, an order numbered) and cancel one (stock back)
// - Confirm the supplies of a shipment when it becomes final; cancel them when it is cancelled
//
// IMPORTANT NOTES:
// - The caller saves; nothing here saves
// - Stock moves only while the company counts its supplies stock
// =========================================================
public sealed class OperationsSupplySales
{
    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsSupplyStockKeeper _keeper;

    private readonly OperationsNumbering _numbering;

    public OperationsSupplySales(IOperationsSupplyRepository supplies, IOperationsSettingsRepository settings, OperationsSupplyStockKeeper keeper, OperationsNumbering numbering)
    {
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
    }

    // The price of the line's supply for its new quantity, from the version the line keeps (S-12); a new line takes today's version.
    public static (Guid PriceId, decimal Unit) Price(OperationsSupply supply, OperationsSupplySaleLine? line, Guid warehouseId, int quantity, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(supply);
        OperationsSupplyPrice? version = line is null ? supply.PriceAt(at) : supply.Prices.FirstOrDefault(item => item.Id == line.PriceId) ?? supply.PriceAt(at);
        return version is null ? throw new DomainException(OperationsSupplySaleErrors.NoPrice) : (version.Id, version.UnitPrice(warehouseId, quantity));
    }

    // Adds to the sale; true when the warehouse has less on hand than the sale now takes (the clerk is warned; refused when the company says so).
    public async Task<bool> AddAsync(OperationsSupplySale sale, OperationsSupply supply, int quantity, OperationsTenantSettings settings, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(supply);
        ArgumentNullException.ThrowIfNull(settings);
        OperationsSupplySaleLine? line = sale.Lines.FirstOrDefault(item => item.SupplyId == supply.Id);
        int wanted = (line?.Quantity ?? 0) + quantity;
        (Guid priceId, decimal unit) = Price(supply, line, sale.WarehouseId, wanted, utcNow);
        bool short_ = await ShortAsync(sale.WarehouseId, supply.Id, wanted, settings, cancellationToken);
        sale.Add(supply, quantity, priceId, unit, utcNow);
        return short_;
    }

    public async Task<bool> CountAsync(OperationsSupplySale sale, OperationsSupply supply, Guid lineId, int quantity, OperationsTenantSettings settings, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sale);
        (Guid _, decimal unit) = Price(supply, sale.Line(lineId), sale.WarehouseId, quantity, utcNow);
        bool short_ = await ShortAsync(sale.WarehouseId, supply.Id, quantity, settings, cancellationToken);
        sale.Count(lineId, quantity, unit, utcNow);
        return short_;
    }

    // Confirmed: each line sold from the counter's warehouse; an order on its own gets its number.
    public async Task ConfirmAsync(OperationsSupplySale sale, Guid? userId, string? reference, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sale);
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        bool counting = settings?.CountSupplyStock ?? true;
        string? number = sale.ShipmentId is null ? await _numbering.NextAsync(OperationsNumberKind.SupplyOrder, cancellationToken) : null;
        sale.Confirm(number, counting, utcNow);

        if (counting)
        {
            foreach (OperationsSupplySaleLine line in sale.Lines)
            {
                await _keeper.MoveAsync(OperationsSupplyMovement.Moved(sale.TenantId, line.SupplyId, sale.WarehouseId, OperationsSupplyMovementKind.Sold, line.Quantity, sale.Id, userId,
                    number ?? reference, utcNow), utcNow, cancellationToken);
            }
        }
    }

    public async Task CancelAsync(OperationsSupplySale sale, Guid? userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sale);

        if (sale.Cancel(utcNow))
        {
            foreach (OperationsSupplySaleLine line in sale.Lines)
            {
                await _keeper.MoveAsync(OperationsSupplyMovement.Moved(sale.TenantId, line.SupplyId, sale.WarehouseId, OperationsSupplyMovementKind.SaleCancelled, line.Quantity, sale.Id, userId,
                    sale.Number, utcNow), utcNow, cancellationToken);
            }
        }
    }

    // A shipment became final: its supplies are sold with it (an empty list is simply cancelled).
    public async Task ConfirmForShipmentAsync(OperationsShipment shipment, Guid? userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (await _supplies.FindShipmentSaleAsync(shipment.Id, cancellationToken) is { Status: OperationsSupplySaleStatus.Open } sale)
        {
            if (sale.Lines.Count == 0)
            {
                sale.Cancel(utcNow);
            }
            else
            {
                await ConfirmAsync(sale, userId, shipment.Number, utcNow, cancellationToken);
            }
        }
    }

    public async Task CancelForShipmentAsync(OperationsShipment shipment, Guid? userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (await _supplies.FindShipmentSaleAsync(shipment.Id, cancellationToken) is { } sale)
        {
            await CancelAsync(sale, userId, utcNow, cancellationToken);
        }
    }

    private async Task<bool> ShortAsync(Guid warehouseId, Guid supplyId, int wanted, OperationsTenantSettings settings, CancellationToken cancellationToken)
    {
        if (!settings.CountSupplyStock)
        {
            return false;
        }

        int onHand = (await _supplies.GetStockAsync(supplyId, warehouseId, cancellationToken))?.OnHand ?? 0;

        if (onHand >= wanted)
        {
            return false;
        }

        return settings.SupplyStockOut == OperationsSupplyStockOut.Refuse ? throw new DomainException(OperationsSupplyStockErrors.OutOfStock) : true;
    }
}
