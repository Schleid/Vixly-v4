using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsSupplyDomainTests.cs
//
// PURPOSE:
// Proves the shipping supplies catalogue rules (Slice 5E, S-1 and S-2).
//
// RESPONSIBILITIES:
// - Item codes and barcodes in capitals, unique within a supply, scanned either way
// - Prices: quantity breaks, warehouse prices and prices from a later day
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsSupplyDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private static OperationsSupplyDetails Box => new(" box-m ", "Medium box", "Boîte moyenne", null, null, OperationsSupplyCategory.Boxes, true);

    [Fact]
    public void Codes_AreKeptInCapitals_DuplicatesCountOnce_AndAScanFindsTheSupplyEitherWay()
    {
        OperationsSupply supply = OperationsSupply.Create(Tenant, Box, new[] { "012345678905", " bx-m-2 ", "BX-M-2", "" }, OperationsSupplyPrice.Create(2.50m, null, null, Now), Now);

        Assert.Equal("BOX-M", supply.Code);
        Assert.Equal(new[] { "012345678905", "BX-M-2" }, supply.Barcodes.Select(barcode => barcode.Code));
        Assert.True(supply.Answers("box-m"));
        Assert.True(supply.Answers("bx-m-2"));
        Assert.False(supply.Answers("tape"));
        Assert.Equal(("Boîte moyenne", "Medium box"), (supply.Name("fr"), supply.Name("es")));
        Assert.Null(supply.Unit);

        supply.ChangeBarcodes(new[] { "BX-M-2", "BX-M-3" }, Now);
        Assert.Equal(new[] { "BX-M-2", "BX-M-3" }, supply.Barcodes.Select(barcode => barcode.Code));
        Assert.Equal("ops.supplies.barcode_invalid", Assert.Throws<DomainException>(() => supply.ChangeBarcodes(new[] { "A B" }, Now)).Error.Code);
        Assert.Equal("ops.supplies.barcode_invalid", Assert.Throws<DomainException>(() => supply.ChangeBarcodes(new[] { "1", "2", "3", "4", "5", "6" }, Now)).Error.Code);
        Assert.Equal("ops.supplies.code_invalid",
            Assert.Throws<DomainException>(() => supply.Change(Box with { Code = "box m" }, true, Now)).Error.Code);
        Assert.Equal("ops.supplies.name_required",
            Assert.Throws<DomainException>(() => supply.Change(Box with { NameEn = " " }, true, Now)).Error.Code);
    }

    // Slice 5E step 2: only movements move the stock; transfers and counts keep their rules.
    [Fact]
    public void Stock_MovesOnlyWithMovements_TransfersAndCountsKeepTheirRules()
    {
        Guid supply = Guid.NewGuid();
        Guid warehouse = Guid.NewGuid();
        Guid clerk = Guid.NewGuid();
        OperationsSupplyStock stock = OperationsSupplyStock.Create(Tenant, supply, warehouse, Now);

        stock.Apply(OperationsSupplyMovement.Received(Tenant, supply, warehouse, 40, 0.85m, "Uline", "INV-1", Guid.NewGuid(), clerk, Now), Now);
        stock.Apply(OperationsSupplyMovement.Moved(Tenant, supply, warehouse, OperationsSupplyMovementKind.Sold, 3, Guid.NewGuid(), clerk, "SH1", Now), Now);
        stock.Apply(OperationsSupplyMovement.Adjusted(Tenant, supply, warehouse, -2, OperationsSupplyAdjustReason.Damaged, null, Guid.NewGuid(), clerk, Now), Now);
        stock.Reorder(35, Now);
        Assert.Equal((35, true), (stock.OnHand, stock.IsLow));

        Assert.Equal("ops.supplies.stock.reason_required",
            Assert.Throws<DomainException>(() => OperationsSupplyMovement.Adjusted(Tenant, supply, warehouse, 1, OperationsSupplyAdjustReason.Correction, " ", Guid.NewGuid(), clerk, Now)).Error.Code);
        Assert.Equal("ops.supplies.stock.quantity_invalid",
            Assert.Throws<DomainException>(() => OperationsSupplyMovement.Received(Tenant, supply, warehouse, 0, null, null, null, Guid.NewGuid(), clerk, Now)).Error.Code);
        Assert.Equal("ops.supplies.stock.lines_invalid",
            Assert.Throws<DomainException>(() => stock.Apply(OperationsSupplyMovement.Received(Tenant, Guid.NewGuid(), warehouse, 1, null, null, null, Guid.NewGuid(), clerk, Now), Now)).Error.Code);

        // A transfer goes to another warehouse, each supply once; received once.
        Assert.Equal("ops.supplies.stock.warehouse_invalid",
            Assert.Throws<DomainException>(() => OperationsSupplyTransfer.Send(Tenant, warehouse, warehouse, new[] { new OperationsSupplyLine(supply, 1) }, null, clerk, Now)).Error.Code);
        Assert.Equal("ops.supplies.stock.lines_invalid", Assert.Throws<DomainException>(() => OperationsSupplyTransfer.Send(Tenant, warehouse, Guid.NewGuid(),
            new[] { new OperationsSupplyLine(supply, 1), new OperationsSupplyLine(supply, 2) }, null, clerk, Now)).Error.Code);
        OperationsSupplyTransfer transfer = OperationsSupplyTransfer.Send(Tenant, warehouse, Guid.NewGuid(), new[] { new OperationsSupplyLine(supply, 5) }, "For Les Cayes", clerk, Now);
        transfer.Receive(clerk, Now);
        Assert.False(transfer.InTransit);
        Assert.Equal("ops.supplies.stock.transfer_done", Assert.Throws<DomainException>(() => transfer.Receive(clerk, Now)).Error.Code);

        // A count takes only its own supplies and nothing once closed.
        OperationsSupplyCount count = OperationsSupplyCount.Open(Tenant, warehouse, new[] { supply }, clerk, Now);
        count.Enter(new Dictionary<Guid, int?> { [supply] = 33 }, Now);
        Assert.Equal(33, count.Lines.Single().Counted);
        Assert.Equal("ops.supplies.stock.quantity_invalid", Assert.Throws<DomainException>(() => count.Enter(new Dictionary<Guid, int?> { [Guid.NewGuid()] = 1 }, Now)).Error.Code);
        count.Close(clerk, false, Now);
        Assert.Equal("ops.supplies.stock.count_closed", Assert.Throws<DomainException>(() => count.Enter(new Dictionary<Guid, int?> { [supply] = 1 }, Now)).Error.Code);
    }

    // Slice 5E step 3: a sale keeps its price, its discounts stay below 100%, and only a confirmed sale gives its stock back.
    [Fact]
    public void Sale_AddsToTheSameLine_DiscountsBelowAHundred_AndOnlyAConfirmedSaleGivesStockBack()
    {
        OperationsSupply box = OperationsSupply.Create(Tenant, Box, Array.Empty<string>(), OperationsSupplyPrice.Create(2.50m, new[] { new OperationsSupplyBreak(10, 2.00m) }, null, Now), Now);
        OperationsSupplySale sale = OperationsSupplySale.Start(Tenant, Guid.NewGuid(), Guid.NewGuid(), null, "USD", Guid.NewGuid(), Now);
        Guid price = box.PriceAt(Now)!.Id;

        OperationsSupplySaleLine line = sale.Add(box, 4, price, 2.50m, Now);
        sale.Add(box, 6, price, 2.00m, Now);
        Assert.Equal((1, 10, 20.00m), (sale.Lines.Count, line.Quantity, sale.Total));

        sale.Discount(line.Id, 12.5m, "Big order", Guid.NewGuid(), null, Now);
        Assert.Equal((2.50m, 17.50m), (sale.DiscountTotal, sale.Total));
        Assert.Equal("ops.supplies.sale.discount_invalid", Assert.Throws<DomainException>(() => sale.Discount(line.Id, 100m, "Free", Guid.NewGuid(), null, Now)).Error.Code);
        sale.Discount(line.Id, 0m, "ignored", Guid.NewGuid(), null, Now);
        Assert.Null(line.DiscountReason);

        Assert.False(sale.Cancel(Now));
        Assert.Equal("ops.supplies.sale.not_open", Assert.Throws<DomainException>(() => sale.Add(box, 1, price, 2.50m, Now)).Error.Code);

        OperationsSupplySale order = OperationsSupplySale.Start(Tenant, Guid.NewGuid(), Guid.NewGuid(), null, "USD", Guid.NewGuid(), Now);
        Assert.Equal("ops.supplies.sale.empty", Assert.Throws<DomainException>(() => order.Confirm("SO26000001", true, Now)).Error.Code);
        order.Add(box, 1, price, 2.50m, Now);
        order.Confirm("SO26000001", true, Now);
        Assert.Equal(("SO26000001", true), (order.Number, order.StockMoved));
        Assert.True(order.Cancel(Now));
    }

    [Fact]
    public void Price_GoesDownWithQuantity_AWarehousePriceReplacesIt_AndALaterPriceStartsOnItsDay()
    {
        Guid remote = Guid.NewGuid();
        OperationsSupplyPrice price = OperationsSupplyPrice.Create(2.50m,
            new[] { new OperationsSupplyBreak(25, 1.80m), new OperationsSupplyBreak(10, 2.00m) },
            new[] { new OperationsSupplyWarehousePrice(remote, 3.00m) }, Now);
        OperationsSupply supply = OperationsSupply.Create(Tenant, Box, Array.Empty<string>(), price, Now);

        Assert.Equal(2.50m, supply.UnitPrice(null, 1, Now));
        Assert.Equal(2.00m, supply.UnitPrice(null, 10, Now));
        Assert.Equal(1.80m, supply.UnitPrice(Guid.NewGuid(), 40, Now));
        Assert.Equal(3.00m, supply.UnitPrice(remote, 40, Now));
        Assert.Null(supply.UnitPrice(null, 1, Now.AddDays(-1)));

        supply.SchedulePrice(OperationsSupplyPrice.Create(2.75m, null, null, Now.AddDays(7)), Now);
        Assert.Equal(2.00m, supply.UnitPrice(null, 10, Now.AddDays(6)));
        Assert.Equal(2.75m, supply.UnitPrice(null, 10, Now.AddDays(7)));

        // Breaks go up in quantity and down in price; a warehouse once.
        Assert.Equal("ops.supplies.breaks_invalid", Assert.Throws<DomainException>(() => OperationsSupplyPrice.Create(2.50m, new[] { new OperationsSupplyBreak(10, 2.60m) }, null, Now)).Error.Code);
        Assert.Equal("ops.supplies.breaks_invalid", Assert.Throws<DomainException>(() => OperationsSupplyPrice.Create(2.50m, new[] { new OperationsSupplyBreak(1, 2.00m) }, null, Now)).Error.Code);
        Assert.Equal("ops.supplies.breaks_invalid",
            Assert.Throws<DomainException>(() => OperationsSupplyPrice.Create(2.50m, new[] { new OperationsSupplyBreak(10, 2.00m), new OperationsSupplyBreak(20, 2.00m) }, null, Now)).Error.Code);
        Assert.Equal("ops.supplies.warehouse_prices_invalid",
            Assert.Throws<DomainException>(() => OperationsSupplyPrice.Create(2.50m, null, new[] { new OperationsSupplyWarehousePrice(remote, 1m), new OperationsSupplyWarehousePrice(remote, 2m) }, Now)).Error.Code);
        Assert.Equal("ops.supplies.price_invalid", Assert.Throws<DomainException>(() => OperationsSupplyPrice.Create(2.555m, null, null, Now)).Error.Code);
    }
}
