using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsBundleDomainTests.cs
//
// PURPOSE:
// Proves the bundle and repack box rules (Slice 6A, D-1 to D-9).
//
// RESPONSIBILITIES:
// - A bundle takes only its destination and service, against its target: warn or refuse
// - Closed once with weight and size, never reopened; voided with a reason
// - A repack box takes its whole shipment before it closes; it goes into a bundle once
// - Packed and voided events move the milestone forward and back
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsBundleDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Warehouse = Guid.NewGuid();

    private static readonly Guid Worker = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static OperationsBundle Bundle(OperationsBundleBasis basis = OperationsBundleBasis.Weight, decimal? target = 10m, decimal? divisor = null, string country = "HT") =>
        OperationsBundle.Create(Tenant, new OperationsBundleDraft(Warehouse, null, country, "air_standard", "Sack", basis, target, divisor), "BD26000001", Worker, null, Now);

    private static OperationsShipment Shipment(string service = "air_standard", OperationsShipmentOptions options = OperationsShipmentOptions.None, string country = "HT")
    {
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Guid.NewGuid(), Warehouse,
            OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, country), "Ana Pierre", "+50937123456", options)
                .ForService(service), Now);
        shipment.DeclareParcelCount(2, 0, Now);
        shipment.Book(40m, "USD", Now);
        return shipment;
    }

    [Fact]
    public void ABundle_TakesItsDestinationAndService_AndWarnsOrRefusesPastItsTarget()
    {
        OperationsBundle bundle = Bundle();

        Assert.Null(bundle.Refuses(Shipment()));
        Assert.Equal(OperationsBundleErrors.WrongService, bundle.Refuses(Shipment("ocean")));
        Assert.Equal(OperationsBundleErrors.WrongDestination, bundle.Refuses(Shipment(country: "DO")));

        Assert.False(bundle.Take(1, 6m, 0m, OperationsBundleTargetRule.Warn, Now));
        Assert.True(bundle.Take(1, 5m, 0m, OperationsBundleTargetRule.Warn, Now));
        Assert.Equal(OperationsBundleErrors.TargetReached, Assert.Throws<DomainException>(() => bundle.Take(1, 1m, 0m, OperationsBundleTargetRule.Refuse, Now)).Error);
        Assert.Equal((2, 11m), (bundle.ParcelCount, bundle.Filled));

        bundle.Release(1, 5m, 0m, Now);
        Assert.Equal((1, 6m), (bundle.ParcelCount, bundle.Filled));

        // Free fill: no target, nothing refused.
        Assert.False(Bundle(OperationsBundleBasis.Free, null).Take(1, 500m, 0m, OperationsBundleTargetRule.Refuse, Now));
    }

    [Fact]
    public void ABundle_IsCheckedWhenMade()
    {
        Assert.Equal(OperationsBundleErrors.DivisorRequired, Assert.Throws<DomainException>(() => Bundle(OperationsBundleBasis.Volumetric, 10m)).Error);
        Assert.Equal(OperationsBundleErrors.TargetInvalid, Assert.Throws<DomainException>(() => Bundle(OperationsBundleBasis.Free, 10m)).Error);
        Assert.Equal(OperationsBundleErrors.TargetInvalid, Assert.Throws<DomainException>(() => Bundle(OperationsBundleBasis.Weight, null)).Error);
        Assert.Equal(OperationsBundleErrors.DestinationRequired, Assert.Throws<DomainException>(() => Bundle(country: "XX")).Error);
        Assert.Equal(OperationsBundleErrors.TypeInvalid, Assert.Throws<DomainException>(() => OperationsBundle.TypeText(new string('x', 31))).Error);

        // Volumetric: the parcels' volume with the service's divisor (cm³ per kg).
        OperationsBundle volumetric = Bundle(OperationsBundleBasis.Volumetric, 20m, 5000m);
        Assert.Equal(10m, volumetric.VolumetricOf(50m, 50m, 20m));
        Assert.True(volumetric.Take(1, 2m, 25m, OperationsBundleTargetRule.Warn, Now));
    }

    [Fact]
    public void ABundle_ClosesOnceWithItsWeightAndSize_AndIsVoidedWithAReason()
    {
        OperationsBundle bundle = Bundle(OperationsBundleBasis.Weight, 30m, 5000m);
        Assert.Equal(OperationsBundleErrors.Empty, Assert.Throws<DomainException>(() => bundle.Close(10m, 50m, 40m, 30m, Worker, Now)).Error);

        bundle.Take(2, 12m, 0m, OperationsBundleTargetRule.Warn, Now);
        Assert.Equal(OperationsBundleErrors.CannotVoid, Assert.Throws<DomainException>(() => bundle.Void("wrong", Worker, Now)).Error);
        Assert.Equal(OperationsBundleErrors.MeasurementsInvalid, Assert.Throws<DomainException>(() => bundle.Close(12.5m, 0m, 40m, 30m, Worker, Now)).Error);

        bundle.Close(12.5m, 100m, 50m, 50m, Worker, Now);
        Assert.Equal((OperationsBundleStatus.Closed, 12.5m, 50m), (bundle.Status, bundle.WeightKg!.Value, bundle.VolumetricKg!.Value));
        Assert.Equal(OperationsBundleErrors.NotOpen, Assert.Throws<DomainException>(() => bundle.Take(1, 1m, 0m, OperationsBundleTargetRule.Warn, Now)).Error);
        Assert.Equal(OperationsBundleErrors.NotOpen, Assert.Throws<DomainException>(() => bundle.Close(12.5m, 100m, 50m, 50m, Worker, Now)).Error);

        Assert.Equal(OperationsBundleErrors.ReasonRequired, Assert.Throws<DomainException>(() => bundle.Void(" ", Worker, Now)).Error);
        bundle.Void("Wrong destination packed", Worker, Now);
        Assert.Equal((OperationsBundleStatus.Voided, "Wrong destination packed"), (bundle.Status, bundle.VoidReason));
        Assert.Equal(OperationsBundleErrors.CannotVoid, Assert.Throws<DomainException>(() => bundle.Void("again", Worker, Now)).Error);

        // An empty open bundle is voided too.
        OperationsBundle empty = Bundle();
        empty.Void("Opened by mistake", Worker, Now);
        Assert.Equal(OperationsBundleStatus.Voided, empty.Status);
    }

    [Fact]
    public void ARepackBox_TakesItsWholeShipment_ThenGoesIntoOneBundle()
    {
        Assert.Equal(OperationsBundleErrors.NotConsolidated, Assert.Throws<DomainException>(() => OperationsRepackBox.Open(Tenant, "CB1", Shipment(), Worker, null, Now)).Error);
        OperationsRepackBox box = OperationsRepackBox.Open(Tenant, "CB1", Shipment(options: OperationsShipmentOptions.Consolidate), Worker, null, Now);
        OperationsMeasurements size = OperationsMeasurements.Create(7m, 50m, 40m, 30m);

        box.Take(3m, Now);
        Assert.Equal(OperationsBundleErrors.BoxIncomplete, Assert.Throws<DomainException>(() => box.Close(size, 2, Worker, Now)).Error);
        Assert.Equal(OperationsBundleErrors.BoxNotClosed, Assert.Throws<DomainException>(() => box.PutInto(Guid.NewGuid(), Now)).Error);

        box.Take(3.5m, Now);
        box.Close(size, 2, Worker, Now);
        Assert.Equal((OperationsRepackBoxStatus.Closed, 2, 6.5m, 7m), (box.Status, box.ParcelCount, box.ParcelsWeightKg, box.WeightKg!.Value));
        Assert.Equal(OperationsBundleErrors.BoxNotOpen, Assert.Throws<DomainException>(() => box.Take(1m, Now)).Error);

        Guid bundle = Guid.NewGuid();
        box.PutInto(bundle, Now);
        Assert.Equal(OperationsBundleErrors.InAnotherBundle, Assert.Throws<DomainException>(() => box.PutInto(Guid.NewGuid(), Now)).Error);
        Assert.Equal(OperationsBundleErrors.CannotVoid, Assert.Throws<DomainException>(() => box.Void("wrong", Worker, Now)).Error);

        box.TakeOut(Now);
        box.Void("Repacked again", Worker, Now);
        Assert.Equal(OperationsRepackBoxStatus.Voided, box.Status);
    }

    [Fact]
    public void AParcel_IsInOneBundle_AndABoxedOneGoesWithItsBox()
    {
        OperationsParcel parcel = OperationsParcel.Create(Tenant, Guid.NewGuid(), Now);
        Guid box = Guid.NewGuid();

        parcel.Box(box, Now);
        Assert.Equal(OperationsBundleErrors.InAnotherBox, Assert.Throws<DomainException>(() => parcel.Box(Guid.NewGuid(), Now)).Error);
        parcel.Pack(Guid.NewGuid(), Now);
        Assert.Equal(OperationsBundleErrors.InAnotherBundle, Assert.Throws<DomainException>(() => parcel.Pack(Guid.NewGuid(), Now)).Error);

        parcel.Unpack(Now);
        parcel.Unbox(Now);
        Assert.Equal(((Guid?)null, (Guid?)null), (parcel.BundleId, parcel.BoxId));
    }

    [Fact]
    public void Packed_MovesTheMilestoneToConsolidated_AndItsVoidMovesItBack()
    {
        Guid parcel = Guid.NewGuid();
        OperationsTrackingEvent booked = Record(parcel, OperationsTrackingCodes.Booked, new());
        OperationsTrackingEvent packed = Record(parcel, OperationsTrackingCodes.Packed, new() { ["bundle"] = "BD26000001" });

        Assert.Equal(OperationsMilestone.Consolidated, OperationsTrackingProjection.Replay(new[] { booked, packed }).Milestone);
        Assert.Equal(OperationsTrackingVisibility.Customer, packed.Visibility);
        Assert.Empty(OperationsTrackingCodes.Find(OperationsTrackingCodes.Packed)!.CustomerArguments);

        OperationsTrackingEvent voided = Record(parcel, OperationsTrackingCodes.EventVoided, new() { ["why"] = "taken_out" }, packed.Id);
        Assert.Equal(OperationsMilestone.Booked, OperationsTrackingProjection.Replay(new[] { booked, packed, voided }).Milestone);
    }

    [Fact]
    public void TheCompany_ChoosesItsBundleTypes_ItsTargetRule_AndItsWay()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal((OperationsBundleTargetRule.Warn, OperationsConsolidationWay.Repack, "BD", "CB"), (settings.BundleTargetRule, settings.ConsolidationWay, settings.BundlePrefix, settings.BoxPrefix));
        Assert.Empty(settings.BundleTypeList());

        settings.ChangeBundles(new[] { " Sack ", "Pallet", null, "" }, OperationsBundleTargetRule.Refuse, OperationsConsolidationWay.KeepTogether, Now);
        Assert.Equal(new[] { "Sack", "Pallet" }, settings.BundleTypeList());
        Assert.Equal((OperationsBundleTargetRule.Refuse, OperationsConsolidationWay.KeepTogether), (settings.BundleTargetRule, settings.ConsolidationWay));
        Assert.Equal(OperationsBundleErrors.SettingsInvalid, Assert.Throws<DomainException>(() =>
            settings.ChangeBundles(new[] { "Sack", "sack" }, OperationsBundleTargetRule.Warn, OperationsConsolidationWay.Repack, Now)).Error);

        Assert.Equal("ops.settings.prefixes_must_differ", Assert.Throws<DomainException>(() =>
            settings.ChangePrefixes("SH", "RC", "INV", "PAY", "C", "PK", "SO", "CN", "SH", "CB", Now)).Error.Code);
        Assert.Equal("BD", settings.Prefix(OperationsNumberKind.Bundle));

        // The ship manager switches a consolidated shipment's way; never an ordinary one.
        OperationsShipment consolidated = Shipment(options: OperationsShipmentOptions.Consolidate);
        consolidated.ChooseConsolidation(OperationsConsolidationWay.KeepTogether, Now);
        Assert.Equal(OperationsConsolidationWay.KeepTogether, consolidated.WayOr(OperationsConsolidationWay.Repack));
        Assert.Equal(OperationsBundleErrors.NotConsolidated, Assert.Throws<DomainException>(() => Shipment().ChooseConsolidation(OperationsConsolidationWay.Repack, Now)).Error);

        // Once started, the way in effect stays with the shipment whatever the company later chooses; a chosen way is kept.
        OperationsShipment started = Shipment(options: OperationsShipmentOptions.Consolidate);
        started.StartConsolidating(OperationsConsolidationWay.Repack, Now);
        started.StartConsolidating(OperationsConsolidationWay.KeepTogether, Now);
        Assert.Equal(OperationsConsolidationWay.Repack, started.ConsolidationWay);
        consolidated.StartConsolidating(OperationsConsolidationWay.Repack, Now);
        Assert.Equal(OperationsConsolidationWay.KeepTogether, consolidated.ConsolidationWay);
    }

    private static OperationsTrackingEvent Record(Guid parcel, string code, Dictionary<string, string> arguments, Guid? voids = null) =>
        OperationsTrackingEvent.Record(Tenant, new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel, code, arguments, Now, "test:" + Guid.NewGuid().ToString("N"),
            OperationsTrackingActor.Staff, OperationsTrackingSource.Scan) { ActorUserId = Worker, VoidsEventId = voids }, Now);
}
