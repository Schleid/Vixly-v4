using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsDockDomainTests.cs
//
// PURPOSE:
// Proves the dock rules of Slice 3B (owner 2026-10-18): carrier barcodes, deliveries and their receipts, printers and print jobs.
//
// RESPONSIBILITIES:
// - The tracking numbers a carrier barcode stands for, and its carrier
// - A delivery closes once; its receipt counts its pieces
// - Printers are checked; a label is printed at most once and only while fresh
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsDockDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Warehouse = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("1z999aa1 0123 456784", "1Z999AA10123456784")]
    [InlineData("]C11Z999AA10123456784", "1Z999AA10123456784")]
    [InlineData("TBA123456789000", "TBA123456789000")]
    [InlineData("420331229400111899223397846256", "9400111899223397846256")]
    [InlineData("4203312212349400111899223397846256", "9400111899223397846256")]
    [InlineData("9632001960000000000400794887123456", "794887123456")]
    [InlineData("9622001900000123456789", "900000123456789")]
    public void CarrierBarcode_GivesTheTrackingNumberFirst(string scanned, string expected)
    {
        IReadOnlyList<string> candidates = OperationsCarrierBarcode.Candidates(scanned);

        Assert.Equal(expected, candidates[0]);
        Assert.Equal(candidates.Count, candidates.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CarrierBarcode_KeepsEveryReadingOfALongFedExCode()
    {
        IReadOnlyList<string> candidates = OperationsCarrierBarcode.Candidates("9632001960000000000400794887123456");

        Assert.Equal(new[] { "794887123456", "400794887123456", "9632001960000000000400794887123456" }, candidates);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB")]
    [InlineData("42033122")]
    [InlineData("420331221234")]
    [InlineData(null)]
    public void CarrierBarcode_ReadsNothingFromNoise(string? scanned)
    {
        Assert.Empty(OperationsCarrierBarcode.Candidates(scanned));
    }

    [Theory]
    [InlineData("1Z999AA10123456784", "UPS")]
    [InlineData("TBA123456789000", "Amazon")]
    [InlineData("9400111899223397846256", "USPS")]
    [InlineData("794887123456", "FedEx")]
    [InlineData("ABCDEFGH12", null)]
    public void CarrierBarcode_NamesTheCarrier(string tracking, string? carrier)
    {
        Assert.Equal(carrier, OperationsCarrierBarcode.CarrierOf(tracking));
    }

    [Fact]
    public void Delivery_ClosesOnce_AndItsReceiptCountsThePieces()
    {
        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(Tenant, Warehouse, " UPS ", Guid.NewGuid(), "key", Now);
        Assert.Equal("UPS", delivery.Carrier);
        OperationsWarehouseReceipt receipt = OperationsWarehouseReceipt.Issue(Tenant, "WR26000001", delivery, Guid.NewGuid(), 12, 1, Now.AddMinutes(20));

        delivery.Close(receipt.Id, Now.AddMinutes(20));

        Assert.Equal(OperationsDeliveryStatus.Closed, delivery.Status);
        Assert.Equal(receipt.Id, delivery.ReceiptId);
        Assert.Equal((12, 1, delivery.Id, Now), (receipt.PieceCount, receipt.DamagedCount, receipt.DeliveryId, receipt.OpenedUtc));
        Assert.Equal("ops.receiving.delivery_closed", Assert.Throws<DomainException>(() => delivery.Close(Guid.NewGuid(), Now)).Error.Code);
        Assert.Equal("ops.receiving.delivery_closed", Assert.Throws<DomainException>(delivery.EnsureOpen).Error.Code);
    }

    [Fact]
    public void Parcel_ArrivesWithItsDelivery_AndWaitsForItsCheck()
    {
        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(Tenant, Warehouse, "UPS", Guid.NewGuid(), "key", Now);
        OperationsParcel parcel = OperationsParcel.Receive(Tenant, "PK26000001", delivery, "UPS", "1Z999AA10123456784", true, Now);

        Assert.Equal(delivery.Id, parcel.DeliveryId);
        Assert.Equal(Warehouse, parcel.WarehouseId);
        Assert.True(parcel.ArrivedDamaged);
        Assert.Null(parcel.WeightKg);

        parcel.Measure(OperationsMeasurements.Create(2.5m, 30, 20, 10), Now.AddMinutes(5));
        Assert.Equal(2.5m, parcel.WeightKg);

        delivery.Close(Guid.NewGuid(), Now);
        Assert.Equal("ops.receiving.delivery_closed", Assert.Throws<DomainException>(() => OperationsParcel.Receive(Tenant, "PK26000002", delivery, "UPS", "1Z999AA10123456785", false, Now)).Error.Code);
    }

    [Theory]
    [InlineData("", OperationsPrinterConnection.Network, "10.0.0.5:9100", 203, "ops.printing.name_invalid")]
    [InlineData("<b>Dock</b>", OperationsPrinterConnection.Network, "10.0.0.5:9100", 203, "ops.printing.name_invalid")]
    [InlineData("Dock 1", OperationsPrinterConnection.Network, "10.0.0.5", 203, "ops.printing.address_invalid")]
    [InlineData("Dock 1", OperationsPrinterConnection.Network, "10.0.0.5:70000", 203, "ops.printing.address_invalid")]
    [InlineData("Dock 1", OperationsPrinterConnection.Network, "10.0.0.5:22", 203, "ops.printing.address_invalid")]
    [InlineData("Dock 1", OperationsPrinterConnection.Network, "10.0.0.5:9110", 203, "ops.printing.address_invalid")]
    [InlineData("Dock 1", OperationsPrinterConnection.Installed, "", 203, "ops.printing.address_invalid")]
    [InlineData("Dock 1", OperationsPrinterConnection.Network, "10.0.0.5:9100", 600, "ops.printing.dpi_invalid")]
    public void Printer_RefusesWhatCannotPrint(string name, OperationsPrinterConnection connection, string address, int dpi, string code)
    {
        Assert.Equal(code, Assert.Throws<DomainException>(() => OperationsLabelPrinter.Create(Tenant, Warehouse, name, connection, address, dpi, Now)).Error.Code);
    }

    [Fact]
    public void Printer_TakesANetworkOrAnInstalledPrinter()
    {
        OperationsLabelPrinter printer = OperationsLabelPrinter.Create(Tenant, Warehouse, " Dock 1 ", OperationsPrinterConnection.Network, "zebra-dock1.local:9100", 300, Now);
        Assert.Equal(("Dock 1", "zebra-dock1.local:9100", 300, true), (printer.Name, printer.Address, printer.Dpi, printer.IsActive));

        printer.Change("Dock 1", OperationsPrinterConnection.Installed, "ZDesigner GK420d", 203, false, Now);
        Assert.Equal((OperationsPrinterConnection.Installed, false), (printer.Connection, printer.IsActive));
    }

    [Fact]
    public void PrintJob_IsPrintedAtMostOnce_AndOnlyWhileFresh()
    {
        OperationsLabelPrinter printer = OperationsLabelPrinter.Create(Tenant, Warehouse, "Dock 1", OperationsPrinterConnection.Network, "10.0.0.5:9100", 203, Now);
        OperationsPrintJob job = OperationsPrintJob.Queue(Tenant, printer, "^XA^XZ", Now);

        Assert.True(job.CanBeTaken(Now.AddSeconds(30)));
        Assert.False(job.CanBeTaken(Now + OperationsPrintJob.FreshFor));

        job.Take(Now.AddSeconds(30));
        Assert.False(job.CanBeTaken(Now.AddSeconds(31)));

        job.Finish(false, "paper_out", Now.AddSeconds(32));
        Assert.Equal((OperationsPrintJobStatus.Failed, "paper_out"), (job.Status, job.ErrorCode));
        Assert.False(job.CanBeTaken(Now.AddSeconds(40)));

        OperationsPrintJob printed = OperationsPrintJob.Queue(Tenant, printer, "^XA^XZ", Now);
        printed.Take(Now);
        printed.Finish(true, null, Now.AddSeconds(1));
        Assert.Equal((OperationsPrintJobStatus.Printed, (string?)null), (printed.Status, printed.ErrorCode));
    }

    [Fact]
    public void PrintAgent_StopsForGood()
    {
        OperationsPrintAgent agent = OperationsPrintAgent.Register(Tenant, Warehouse, "Dock PC 1", "hash", Now);
        agent.Seen(Now.AddMinutes(1));

        agent.Stop(Now.AddMinutes(2));

        Assert.False(agent.IsActive);
        Assert.Equal(Now.AddMinutes(1), agent.LastSeenUtc);
    }
}
