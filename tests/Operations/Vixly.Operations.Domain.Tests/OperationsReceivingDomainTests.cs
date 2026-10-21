using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsReceivingDomainTests.cs
//
// PURPOSE:
// Proves the receiving rules of Slice 3B (ADR-0022, design section 4).
//
// RESPONSIBILITIES:
// - Tracking numbers, measurements, texts and shelf codes are checked
// - Pre-alerts open, match and close once
// - A parcel joins only an open shipment of its own warehouse and tenant, and a shelf of its warehouse
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsReceivingDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Warehouse = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("1z999aa1 0123-456784", "1Z999AA10123456784")]
    [InlineData(" tba123456 ", "TBA123456")]
    public void TrackingNumbers_AreNormalised(string typed, string stored) =>
        Assert.Equal(stored, OperationsCarrierTracking.Normalize(typed));

    [Theory]
    [InlineData(null)]
    [InlineData("1234")]
    [InlineData("1Z999AA1012345678<")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDE")]
    public void BadTrackingNumbers_AreRefused(string? typed)
    {
        Assert.False(OperationsCarrierTracking.TryNormalize(typed, out string normalized));
        Assert.Empty(normalized);
    }

    [Theory]
    [InlineData(0, 10, 10, 10)]
    [InlineData(1001, 10, 10, 10)]
    [InlineData(1, 401, 10, 10)]
    [InlineData(1, 10, -1, 10)]
    public void Measurements_OutsideTheLimits_AreRefused(decimal kg, decimal length, decimal width, decimal height) =>
        Assert.Equal("ops.receiving.measurements_invalid", Assert.Throws<DomainException>(() => OperationsMeasurements.Create(kg, length, width, height)).Error.Code);

    [Fact]
    public void Measurements_AreRounded()
    {
        OperationsMeasurements measurements = OperationsMeasurements.Create(1.23456m, 30.04m, 20.06m, 10m);
        Assert.Equal(1.235m, measurements.WeightKg);
        Assert.Equal(30.0m, measurements.LengthCm);
        Assert.Equal(20.1m, measurements.WidthCm);
    }

    [Theory]
    [InlineData("<b>shoes</b>")]
    [InlineData("see https://example.com")]
    [InlineData("tab\tinside")]
    public void Declarations_RefuseMarkupLinksAndControls(string contents) =>
        Assert.Equal("ops.receiving.text_too_long", Assert.Throws<DomainException>(() => PreAlert(contents: contents)).Error.Code);

    [Fact]
    public void PreAlert_IsReceivedOrCancelledOnce()
    {
        OperationsPreAlert received = PreAlert();
        Assert.Equal(OperationsPreAlertStatus.Open, received.Status);
        Assert.Equal("1Z999AA10123456784", received.TrackingNumber);
        received.MarkReceived(Guid.NewGuid(), Now);
        Assert.Equal("ops.receiving.pre_alert_not_open", Assert.Throws<DomainException>(() => received.Cancel(Now)).Error.Code);

        OperationsPreAlert cancelled = PreAlert();
        cancelled.Cancel(Now);
        Assert.Equal("ops.receiving.pre_alert_not_open", Assert.Throws<DomainException>(() => cancelled.MarkReceived(Guid.NewGuid(), Now)).Error.Code);
        Assert.Equal("ops.receiving.value_invalid", Assert.Throws<DomainException>(() => PreAlert(value: -1m)).Error.Code);
    }

    [Fact]
    public void Parcel_TakesTheCustomersDeclaration_FromThePreAlert()
    {
        OperationsParcel parcel = Received();
        Guid customer = Guid.NewGuid();
        OperationsShipment shipment = OperationsShipment.Open(Tenant, customer, Warehouse, Now);
        parcel.JoinShipment(shipment, Now);

        // Another customer's announcement of the same number is never taken (review M5).
        Assert.Equal("ops.receiving.pre_alert_not_open", Assert.Throws<DomainException>(() => parcel.MatchPreAlert(PreAlert(), Now)).Error.Code);

        OperationsPreAlert preAlert = PreAlert(customer: customer);
        parcel.MatchPreAlert(preAlert, Now);

        Assert.Equal(shipment.CustomerId, parcel.CustomerId);
        Assert.Equal(shipment.Id, parcel.ShipmentId);
        Assert.Equal("Two pairs of shoes", parcel.DeclaredContents);
        Assert.Equal(120m, parcel.DeclaredValue);
        Assert.Equal(parcel.Id, preAlert.ParcelId);
        Assert.Equal(OperationsPreAlertStatus.Received, preAlert.Status);
    }

    [Fact]
    public void Parcel_JoinsOnlyAnOpenShipmentOfItsWarehouseAndTenant()
    {
        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => Received().JoinShipment(OperationsShipment.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Now), Now)).Error.Code);
        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => Received().JoinShipment(OperationsShipment.Open(Guid.NewGuid(), Guid.NewGuid(), Warehouse, Now), Now)).Error.Code);
    }

    [Fact]
    public void Parcel_GoesOnlyOnAnActiveShelfOfItsWarehouse()
    {
        OperationsStorageLocation shelf = OperationsStorageLocation.Create(Tenant, Warehouse, " a-03-2 ", OperationsStorageZone.Storage, Now);
        Assert.Equal("A-03-2", shelf.Code);
        OperationsParcel parcel = Received();
        parcel.PutAway(shelf, Now);
        Assert.Equal(shelf.Id, parcel.StorageLocationId);

        OperationsStorageLocation elsewhere = OperationsStorageLocation.Create(Tenant, Guid.NewGuid(), "B-1", OperationsStorageZone.Storage, Now);
        Assert.Equal("ops.receiving.location_inactive", Assert.Throws<DomainException>(() => parcel.PutAway(elsewhere, Now)).Error.Code);
        shelf.Change(OperationsStorageZone.Hold, false, Now);
        Assert.Equal("ops.receiving.location_inactive", Assert.Throws<DomainException>(() => parcel.PutAway(shelf, Now)).Error.Code);
    }

    [Fact]
    public void Parcel_HoldsItsOriginKey_OnlyWhileAtTheOrigin()
    {
        OperationsParcel parcel = Received();
        Assert.Equal(Warehouse.ToString("N") + ":1Z999AA10123456784", parcel.OriginKey);

        parcel.Project(new OperationsParcelState(OperationsMilestone.Booked, OperationsParcelFlags.None, OperationsHoldReasons.None, Now), Now);
        Assert.True(parcel.IsAtOrigin);
        Assert.NotNull(parcel.OriginKey);

        parcel.Project(new OperationsParcelState(OperationsMilestone.DepartedOrigin, OperationsParcelFlags.None, OperationsHoldReasons.None, Now), Now);
        Assert.False(parcel.IsAtOrigin);
        Assert.Null(parcel.OriginKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-A1")]
    [InlineData("A 1")]
    [InlineData("A1234567890123456789X")]
    public void ShelfCodes_AreChecked(string code) =>
        Assert.Equal("ops.receiving.location_code_invalid", Assert.Throws<DomainException>(() => OperationsStorageLocation.Create(Tenant, Warehouse, code, OperationsStorageZone.Storage, Now)).Error.Code);

    [Fact]
    public void Catalogue_KnowsWhatProvesAnUnidentifiedParcel()
    {
        OperationsTrackingCode assigned = OperationsTrackingCodes.Find(OperationsTrackingCodes.CustomerAssigned)!;
        Assert.Equal(new[] { "tracking_number", "sender", "order_number", "label_code" }, assigned.Choices["verified_by"]);
    }

    private static OperationsParcel Received() =>
        OperationsParcel.Receive(Tenant, "PK26000001", OperationsInboundDelivery.Open(Tenant, Warehouse, "UPS", Guid.NewGuid(), "open-key-1", Now), "UPS", "1Z999AA10123456784", false, Now);

    private static OperationsPreAlert PreAlert(string contents = "Two pairs of shoes", decimal value = 120m, Guid? customer = null) =>
        OperationsPreAlert.Create(Instructed(customer ?? Guid.NewGuid()), "UPS", "1z999aa10123456784", "Shoe store", contents, value, "USD", null, Now);

    // A customer's shipment with its instructions: home delivery in Haiti.
    private static OperationsShipment Instructed(Guid customer) =>
        OperationsShipment.Request(Tenant, customer, Warehouse, OperationsShipmentInstructions.HomeDelivery(
            Vixly.Operations.Domain.Locations.OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), "Ana Pierre", "+50937123456",
            OperationsShipmentOptions.None), Now);
}
