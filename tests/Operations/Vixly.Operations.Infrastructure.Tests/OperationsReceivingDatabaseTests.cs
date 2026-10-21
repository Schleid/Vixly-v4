using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsReceivingDatabaseTests.cs
//
// PURPOSE:
// Proves the receiving tables and queries of Slice 3B on SQL Server.
//
// RESPONSIBILITIES:
// - One open pre-alert per tracking number, one open shipment per customer and warehouse, one shelf code per warehouse
// - Parcel lists keep to the given warehouses and filters; another tenant sees nothing
// - One delivery per opening reference, one receipt per delivery; the print program gets only fresh labels
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER
// =========================================================
public sealed class OperationsReceivingDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private readonly OperationsDatabaseFixture _database;

    private readonly Dictionary<Guid, OperationsShipment> _shipments = new();

    public OperationsReceivingDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task TheDatabase_RefusesSecondOpenRecords()
    {
        Guid tenant = Guid.NewGuid();
        (Guid customer, Guid warehouse, _) = await SeedAsync(tenant, "C00001");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<Guid, Guid> warehouseOf = new() { [tenant] = warehouse };

        Guid first = await AddAsync(tenant, Announced(tenant, customer, warehouseOf[tenant], now));
        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(tenant, Announced(tenant, customer, warehouseOf[tenant], now)));

        // Another customer of the company may announce the same number: it never blocks the real owner (review H1).
        Guid neighbour = await CustomerAsync(tenant, "ben@example.com", "C00002");
        await AddAsync(tenant, Announced(tenant, neighbour, warehouseOf[tenant], now));

        // Withdrawn, the number may be announced again; another company may announce it too.
        await using (OperationsDbContext context = _database.NewContext(tenant))
        {
            (await context.PreAlerts.SingleAsync(item => item.Id == first)).Cancel(now);
            await context.SaveChangesAsync();
        }

        await AddAsync(tenant, Announced(tenant, customer, warehouseOf[tenant], now));
        Guid other = Guid.NewGuid();
        (Guid otherCustomer, Guid otherWarehouse, _) = await SeedAsync(other, "C00001");
        warehouseOf[other] = otherWarehouse;
        await AddAsync(other, Announced(other, otherCustomer, warehouseOf[other], now));

        await AddAsync(tenant, OperationsShipment.Open(tenant, customer, warehouse, now));
        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(tenant, OperationsShipment.Open(tenant, customer, warehouse, now)));

        // Shipments the customer created with their instructions are not limited (owner, 2026-10-18).
        await AddAsync(tenant, Instructed(tenant, customer, warehouse, now));
        await AddAsync(tenant, Instructed(tenant, customer, warehouse, now));

        await AddAsync(tenant, OperationsStorageLocation.Create(tenant, warehouse, "A-01", OperationsStorageZone.Storage, now));
        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(tenant, OperationsStorageLocation.Create(tenant, warehouse, "a-01", OperationsStorageZone.Hold, now)));

        // One parcel per number at a warehouse while it is there (review H2); after it leaves, the number may come again.
        Guid parcel = await AddAsync(tenant, Parcel(tenant, "PK26000010", warehouse, "TBA555666777", now));
        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(tenant, Parcel(tenant, "PK26000011", warehouse, "TBA555666777", now)));

        await using (OperationsDbContext context = _database.NewContext(tenant))
        {
            (await context.Parcels.SingleAsync(item => item.Id == parcel)).Project(new OperationsParcelState(OperationsMilestone.DepartedOrigin, OperationsParcelFlags.None, OperationsHoldReasons.None, now), now);
            await context.SaveChangesAsync();
        }

        await AddAsync(tenant, Parcel(tenant, "PK26000012", warehouse, "TBA555666777", now));
    }

    [SqlServerFact]
    public async Task ParcelLists_KeepToTheirWarehousesAndFilters()
    {
        Guid tenant = Guid.NewGuid();
        (Guid customer, Guid warehouse, Guid elsewhere) = await SeedAsync(tenant, "C00042");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (OperationsDbContext context = _database.NewContext(tenant))
        {
            OperationsShipment shipment = OperationsShipment.Open(tenant, customer, warehouse, now);
            OperationsParcel owned = Parcel(tenant, "PK26000001", warehouse, "1Z999AA10123456784", now);
            owned.JoinShipment(shipment, now);
            OperationsParcel orphan = Parcel(tenant, "PK26000002", warehouse, "TBA123456789000", now);
            orphan.Project(new OperationsParcelState(OperationsMilestone.ReceivedAtOrigin, OperationsParcelFlags.OnHold, OperationsHoldReasons.Unidentified, now), now);
            OperationsParcel third = Parcel(tenant, "PK26000003", elsewhere, "9400111899223397846256", now);
            context.AddRange(shipment, owned, orphan, third, Delivery(tenant, owned), Delivery(tenant, orphan), Delivery(tenant, third));
            await context.SaveChangesAsync();
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsReceivingRepository parcels = new(read);

        Assert.Equal(new[] { "PK26000001", "PK26000002" }, (await parcels.ListParcelsAsync(new([warehouse], null, false, false), 1, 25, CancellationToken.None)).Items.Select(item => item.Number).Order());
        Assert.Equal("PK26000002", Assert.Single((await parcels.ListParcelsAsync(new([warehouse], null, true, false), 1, 25, CancellationToken.None)).Items).Number);
        Assert.Equal("PK26000002", Assert.Single((await parcels.ListParcelsAsync(new([warehouse], null, false, true), 1, 25, CancellationToken.None)).Items).Number);
        Assert.Equal("PK26000001", Assert.Single((await parcels.ListParcelsAsync(new([warehouse, elsewhere], "c00042", false, false), 1, 25, CancellationToken.None)).Items).Number);
        Assert.Equal("PK26000001", Assert.Single((await parcels.ListParcelsAsync(new([warehouse], "3456784", false, false), 1, 25, CancellationToken.None)).Items).Number);
        Assert.Empty((await parcels.ListParcelsAsync(new([warehouse], "9400111899223397846256", false, false), 1, 25, CancellationToken.None)).Items);
        Assert.Empty((await parcels.ListParcelsAsync(new(Array.Empty<Guid>(), null, false, false), 1, 25, CancellationToken.None)).Items);

        Assert.NotNull(await parcels.FindActiveParcelAsync([warehouse], ["1Z999AA10123456784"], CancellationToken.None));
        Assert.Equal("PK26000002", (await parcels.FindActiveParcelAsync([elsewhere, warehouse], ["9999", "TBA123456789000"], CancellationToken.None))?.Number);
        Assert.Null(await parcels.FindActiveParcelAsync([elsewhere], ["1Z999AA10123456784"], CancellationToken.None));
        Assert.Null(await parcels.FindActiveParcelAsync([warehouse], Array.Empty<string>(), CancellationToken.None));
        Assert.NotNull(await parcels.FindOpenShipmentAsync(customer, warehouse, CancellationToken.None));

        await using OperationsDbContext stranger = _database.NewContext(Guid.NewGuid());
        Assert.Empty((await new OperationsReceivingRepository(stranger).ListParcelsAsync(new([warehouse, elsewhere], null, false, false), 1, 25, CancellationToken.None)).Items);
    }

    [SqlServerFact]
    public async Task Dock_OneDeliveryPerReference_OneReceiptPerDelivery_AndOnlyFreshLabels()
    {
        Guid tenant = Guid.NewGuid();
        (Guid _, Guid warehouse, _) = await SeedAsync(tenant, "C00077");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(tenant, warehouse, "UPS", Guid.NewGuid(), "open-1", now);
        await AddAsync(tenant, delivery);
        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(tenant, OperationsInboundDelivery.Open(tenant, warehouse, "UPS", Guid.NewGuid(), "open-1", now)));
        Guid other = Guid.NewGuid();
        await AddAsync(other, OperationsInboundDelivery.Open(other, warehouse, "UPS", Guid.NewGuid(), "open-1", now));

        await AddAsync(tenant, OperationsWarehouseReceipt.Issue(tenant, "WR26000001", delivery, Guid.NewGuid(), 3, 0, now));
        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(tenant, OperationsWarehouseReceipt.Issue(tenant, "WR26000002", delivery, Guid.NewGuid(), 3, 0, now)));

        OperationsLabelPrinter printer = OperationsLabelPrinter.Create(tenant, warehouse, "Dock 1", OperationsPrinterConnection.Network, "10.0.0.5:9100", 203, now);
        OperationsPrintJob fresh = OperationsPrintJob.Queue(tenant, printer, "^XA^FDFRESH^FS^XZ", now);
        OperationsPrintJob taken = OperationsPrintJob.Queue(tenant, printer, "^XA^FDTAKEN^FS^XZ", now);
        taken.Take(now);
        OperationsPrintJob stale = OperationsPrintJob.Queue(tenant, printer, "^XA^FDSTALE^FS^XZ", now - OperationsPrintJob.FreshFor - TimeSpan.FromSeconds(1));

        await using (OperationsDbContext context = _database.NewContext(tenant))
        {
            context.AddRange(printer, fresh, taken, stale);
            await context.SaveChangesAsync();
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        Assert.Equal(fresh.Id, Assert.Single(await new OperationsPrintingRepository(read).ListWaitingJobsAsync(warehouse, now, 10, CancellationToken.None)).Id);
        Assert.NotNull(await new OperationsReceivingRepository(read).FindDeliveryByOpenKeyAsync("open-1", CancellationToken.None));

        await using OperationsDbContext stranger = _database.NewContext(Guid.NewGuid());
        Assert.Empty(await new OperationsPrintingRepository(stranger).ListWaitingJobsAsync(warehouse, now, 10, CancellationToken.None));
    }

    // A customer's shipment with its instructions: home delivery in Haiti.
    private static OperationsShipment Instructed(Guid tenant, Guid customer, Guid warehouse, DateTimeOffset now) =>
        OperationsShipment.Request(tenant, customer, warehouse, OperationsShipmentInstructions.HomeDelivery(
            OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), "Ana Pierre", "+50937123456", OperationsShipmentOptions.None), now);

    // A pre-alert in its own new shipment; AddAsync saves the shipment with it.
    private OperationsPreAlert Announced(Guid tenant, Guid customer, Guid warehouse, DateTimeOffset now)
    {
        OperationsShipment shipment = Instructed(tenant, customer, warehouse, now);
        OperationsPreAlert preAlert = OperationsPreAlert.Create(shipment, "UPS", "1Z999AA10123456784", null, "Shoes", 10m, "USD", null, now);
        _shipments[preAlert.Id] = shipment;
        return preAlert;
    }

    // Each test parcel arrives with its own carrier delivery, added together with it.
    private static OperationsParcel Parcel(Guid tenant, string number, Guid warehouse, string tracking, DateTimeOffset now) =>
        OperationsParcel.Receive(tenant, number, OperationsInboundDelivery.Open(tenant, warehouse, "UPS", Guid.NewGuid(), Guid.NewGuid().ToString("N"), now), "UPS", tracking, false, now);

    // The delivery a test parcel names, so the foreign key holds.
    private static OperationsInboundDelivery Delivery(Guid tenant, OperationsParcel parcel)
    {
        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(tenant, parcel.WarehouseId!.Value, "UPS", Guid.NewGuid(), Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow);
        typeof(Vixly.Shared.Kernel.Domain.AuditableEntity).GetProperty(nameof(delivery.Id))!.SetValue(delivery, parcel.DeliveryId);
        return delivery;
    }

    private async Task<Guid> AddAsync(Guid tenant, object entity)
    {
        await using OperationsDbContext context = _database.NewContext(tenant);
        context.Add(entity);

        if (entity is OperationsParcel parcel)
        {
            context.Add(Delivery(tenant, parcel));
        }

        if (entity is OperationsPreAlert preAlert && _shipments.Remove(preAlert.Id, out OperationsShipment? shipment))
        {
            context.Add(shipment);
        }
        await context.SaveChangesAsync();
        return ((Vixly.Shared.Kernel.Domain.AuditableEntity)entity).Id;
    }

    private async Task<Guid> CustomerAsync(Guid tenant, string email, string code)
    {
        await using OperationsDbContext context = _database.NewContext(tenant);
        OperationsCustomer customer = OperationsCustomer.SignUp(tenant, "Ben", "Joseph", null, EmailAddress.Create(email), "+50937123457", LanguageCode.En, "$2a$12$hash", DateTimeOffset.UtcNow);
        customer.ConfirmEmail(code, DateTimeOffset.UtcNow);
        context.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<(Guid Customer, Guid Warehouse, Guid Elsewhere)> SeedAsync(Guid tenant, string code)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using OperationsDbContext context = _database.NewContext(tenant);
        OperationsCustomer customer = OperationsCustomer.SignUp(tenant, "Ana", "Pierre", null, EmailAddress.Create("ana@example.com"), "+50937123456", LanguageCode.En, "$2a$12$hash", now);
        customer.ConfirmEmail(code, now);
        OperationsBranch branch = OperationsBranch.Create(tenant, Details("MIA"), OperationsBranchSide.Origin, now);
        OperationsWarehouse warehouse = OperationsWarehouse.Create(tenant, branch.Id, Details("WH1"), OperationsWarehouseRoles.Receiving, true, now);
        OperationsWarehouse elsewhere = OperationsWarehouse.Create(tenant, branch.Id, Details("WH2"), OperationsWarehouseRoles.Receiving, true, now);
        context.AddRange(customer, branch, warehouse, elsewhere);
        await context.SaveChangesAsync();
        return (customer.Id, warehouse.Id, elsewhere.Id);
    }

    private static OperationsLocationDetails Details(string code) =>
        new(code, code, OperationsPostalAddress.Create("8400 NW 25th St", null, "Doral", "FL", "33122", "US"), null, null, null, true);
}
