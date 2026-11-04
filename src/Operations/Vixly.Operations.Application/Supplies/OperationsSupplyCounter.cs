using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyCounter.cs
//
// PURPOSE:
// Finds the supplies sale a clerk works on at the counter (Slice 5E, S-11 to S-14): a shipment's, or an order on its own.
//
// RESPONSIBILITIES:
// - Check the clerk serves at the warehouse of the sale
// - Start a shipment's sale when its first supply is added, while the shipment is open and not final
// - Refuse a final shipment (S-13)
//
// IMPORTANT NOTES:
// - ops.counter.serve, per warehouse (OperationsCounterDesk)
// - The sale and the shipment come back tracked
// =========================================================
public sealed class OperationsSupplyCounter
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    public OperationsSupplyCounter(OperationsCounterDesk desk, IOperationsSupplyRepository supplies, IOperationsReceivingRepository receiving, IOperationsSettingsRepository settings)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public sealed record Opened(OperationsAccessView Access, OperationsSupplySale Sale, OperationsWarehouse Warehouse, OperationsShipment? Shipment, OperationsTenantSettings Settings);

    // A sale by its id; a shipment's sale only while the shipment is open.
    public async Task<(Opened? Opened, Error? Refused)> SaleAsync(OperationsAuthorizationContext context, Guid saleId, CancellationToken cancellationToken)
    {
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return (null, refused);
        }

        if (await _supplies.GetSaleAsync(saleId, cancellationToken) is not { } sale || counters.FirstOrDefault(counter => counter.Id == sale.WarehouseId) is not { } warehouse)
        {
            return (null, OperationsSupplySaleErrors.NotFound);
        }

        OperationsShipment? shipment = sale.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;
        return (new Opened(access!, sale, warehouse, shipment, await SettingsAsync(context, cancellationToken)), null);
    }

    // The shipment's sale, started when asked and the shipment is open and not final.
    public async Task<(Opened? Opened, Error? Refused)> ShipmentAsync(OperationsAuthorizationContext context, Guid shipmentId, bool start, Guid byUserId, CancellationToken cancellationToken)
    {
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return (null, refused);
        }

        if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is not { HasInstructions: true } shipment
            || counters.FirstOrDefault(counter => counter.Id == shipment.OriginWarehouseId) is not { } warehouse)
        {
            return (null, OperationsShipmentErrors.NotFound);
        }

        if (shipment.Status != OperationsShipmentStatus.Open)
        {
            return (null, OperationsSupplySaleErrors.ShipmentFinal);
        }

        OperationsTenantSettings settings = await SettingsAsync(context, cancellationToken);
        OperationsSupplySale? sale = await _supplies.FindShipmentSaleAsync(shipment.Id, cancellationToken);

        if (sale is null && start)
        {
            sale = OperationsSupplySale.Start(context.RequiredTenantId, shipment.CustomerId, warehouse.Id, shipment.Id, settings.CurrencyCode, byUserId, DateTimeOffset.UtcNow);
            _supplies.Add(sale);
        }

        return sale is null ? (null, OperationsSupplySaleErrors.NotFound) : (new Opened(access!, sale, warehouse, shipment, settings), null);
    }

    // A shipment's supplies change only while it is open; an order only while it is open.
    public static Error? Changeable(Opened opened)
    {
        ArgumentNullException.ThrowIfNull(opened);
        return opened.Shipment is { Status: not OperationsShipmentStatus.Open } ? OperationsSupplySaleErrors.ShipmentFinal
            : opened.Sale.Status != OperationsSupplySaleStatus.Open ? OperationsSupplySaleErrors.NotOpen
            : null;
    }

    private async Task<OperationsTenantSettings> SettingsAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken) =>
        await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, DateTimeOffset.UtcNow);
}
