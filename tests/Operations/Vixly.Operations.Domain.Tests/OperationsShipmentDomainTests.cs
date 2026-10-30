using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsShipmentDomainTests.cs
//
// PURPOSE:
// Proves the customer's shipment rules (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Pickup only at an active destination branch; a recipient with a name and a phone in international form
// - Instructions change while the shipment is open; a cancelled shipment takes nothing more
// - Packages are announced only in a shipment with instructions; a parcel never joins another customer's shipment
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsShipmentDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Warehouse = Guid.NewGuid();

    private static readonly Guid Customer = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pickup_IsAtAnActiveDestinationBranch_AndGivesItsCountry()
    {
        OperationsShipmentInstructions pickup = OperationsShipmentInstructions.Pickup(Branch(OperationsBranchSide.Destination), " Ana Pierre ", "+509 3712 3456",
            OperationsShipmentOptions.Consolidate | OperationsShipmentOptions.Insured);

        Assert.Equal(("HT", "Ana Pierre", "+50937123456"), (pickup.CountryCode, pickup.RecipientName, pickup.RecipientPhone));
        Assert.Equal("ops.shipments.destination_invalid",
            Assert.Throws<DomainException>(() => OperationsShipmentInstructions.Pickup(Branch(OperationsBranchSide.Origin), "Ana", "+50937123456", OperationsShipmentOptions.None)).Error.Code);
    }

    [Theory]
    [InlineData("", "+50937123456", "ops.shipments.recipient_invalid")]
    [InlineData("<b>Ana</b>", "+50937123456", "ops.shipments.recipient_invalid")]
    [InlineData("Ana", "37123456", "ops.shipments.recipient_phone_invalid")]
    [InlineData("Ana", "call me", "ops.shipments.recipient_phone_invalid")]
    public void Recipient_NeedsANameAndAnInternationalPhone(string name, string phone, string code)
    {
        Assert.Equal(code, Assert.Throws<DomainException>(() => Home(name, phone)).Error.Code);
    }

    [Fact]
    public void Shipment_ChangesWhileOpen_AndACancelledOneTakesNothing()
    {
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Ana Pierre", "+50937123456"), Now);
        Assert.True(shipment.HasInstructions);

        shipment.ChangeInstructions(OperationsShipmentInstructions.Pickup(Branch(OperationsBranchSide.Both), "Marie Pierre", "+50937123457", OperationsShipmentOptions.Refrigerated), Now);
        Assert.Equal(("Marie Pierre", OperationsShipmentOptions.Refrigerated), (shipment.RecipientName, shipment.Options));
        Assert.Null(shipment.DeliveryAddress);

        shipment.Cancel(Now);
        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => shipment.ChangeInstructions(Home("Ana", "+50937123456"), Now)).Error.Code);
        Assert.Equal("ops.receiving.shipment_not_open",
            Assert.Throws<DomainException>(() => OperationsPreAlert.Create(shipment, "UPS", "1Z999AA10123456784", null, "Shoes", 10m, "USD", null, Now)).Error.Code);
    }

    [Fact]
    public void Packages_AreAnnouncedOnlyWithInstructions_AndNeverJoinAnotherCustomersShipment()
    {
        OperationsShipment waiting = OperationsShipment.Open(Tenant, Customer, Warehouse, Now);
        Assert.False(waiting.HasInstructions);
        Assert.Equal("ops.shipments.instructions_required",
            Assert.Throws<DomainException>(() => OperationsPreAlert.Create(waiting, "UPS", "1Z999AA10123456784", null, "Shoes", 10m, "USD", null, Now)).Error.Code);

        OperationsShipment mine = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Ana Pierre", "+50937123456"), Now);
        OperationsPreAlert preAlert = OperationsPreAlert.Create(mine, "UPS", "1Z999AA10123456784", null, "Shoes", 10m, "USD", null, Now);
        Assert.Equal((mine.Id, Customer), (preAlert.ShipmentId, preAlert.CustomerId));

        OperationsParcel parcel = OperationsParcel.Receive(Tenant, "PK26000001", OperationsInboundDelivery.Open(Tenant, Warehouse, "UPS", Guid.NewGuid(), "key", Now), "UPS",
            "1Z999AA10123456784", false, Now);
        parcel.JoinShipment(waiting, Now);
        parcel.JoinShipment(mine, Now);
        Assert.Equal(mine.Id, parcel.ShipmentId);

        OperationsShipment neighbours = OperationsShipment.Request(Tenant, Guid.NewGuid(), Warehouse, Home("Ben", "+50937123499"), Now);
        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => parcel.JoinShipment(neighbours, Now)).Error.Code);
    }

    // Slice 4D (owner, 2026-10-19): a pre-alert first, for a warehouse; then into a shipment from there, moved, or taken out; the service travels with the instructions.
    [Fact]
    public void PreAlerts_ComeFirst_AndMoveBetweenTheCustomersShipmentsFromTheirWarehouse()
    {
        OperationsPreAlert preAlert = OperationsPreAlert.Announce(Tenant, Customer, Warehouse, "Amazon", "TBA0004440001", null, "Tape", 12m, "USD", null, Now);
        Assert.Equal((Warehouse, (Guid?)null, false), (preAlert.WarehouseId!.Value, preAlert.ShipmentId, preAlert.IsPlaced));

        OperationsShipment peru = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Ana Pierre", "+50937123456").ForService("air_standard"), Now);
        OperationsShipment dominican = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Luis Gomez", "+18095550101"), Now);
        Assert.Equal(("air_standard", (string?)null), (peru.ServiceCode, dominican.ServiceCode));

        preAlert.PlaceIn(peru, Now);
        Assert.Equal(peru.Id, preAlert.ShipmentId);
        preAlert.PlaceIn(dominican, Now);
        Assert.Equal(dominican.Id, preAlert.ShipmentId);
        preAlert.TakeOut(Now);
        Assert.Null(preAlert.ShipmentId);

        // Never into another customer's shipment, one from another warehouse, or the waiting shipment.
        Assert.Equal("ops.shipments.instructions_required", Assert.Throws<DomainException>(() =>
            preAlert.PlaceIn(OperationsShipment.Request(Tenant, Guid.NewGuid(), Warehouse, Home("Ben", "+50937123499"), Now), Now)).Error.Code);
        Assert.Equal("ops.shipments.parcel_elsewhere", Assert.Throws<DomainException>(() =>
            preAlert.PlaceIn(OperationsShipment.Request(Tenant, Customer, Guid.NewGuid(), Home("Ana", "+50937123456"), Now), Now)).Error.Code);
        Assert.Equal("ops.shipments.instructions_required", Assert.Throws<DomainException>(() => preAlert.PlaceIn(OperationsShipment.Open(Tenant, Customer, Warehouse, Now), Now)).Error.Code);

        // Received at another warehouse than announced: it belongs there, out of a shipment from elsewhere.
        preAlert.PlaceIn(peru, Now);
        Guid elsewhere = Guid.NewGuid();
        preAlert.MarkReceived(Guid.NewGuid(), elsewhere, Now);
        Assert.Equal((elsewhere, (Guid?)null), (preAlert.WarehouseId!.Value, preAlert.ShipmentId));

        Assert.Equal("ops.shipments.origin_invalid", Assert.Throws<DomainException>(() =>
            OperationsPreAlert.Announce(Tenant, Customer, Guid.Empty, "UPS", "1Z999AA10123456784", null, "Shoes", 10m, "USD", null, Now)).Error.Code);
    }

    // Slice 5C (owner, 2026-10-20): the count is 1 to 99 and never below the parcels in; booked needs a service and a count, then nothing changes.
    [Fact]
    public void Shipment_CountsItsParcels_AndOnceBookedIsLocked()
    {
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Ana Pierre", "+50937123456"), Now);
        Assert.Equal("ops.shipments.not_ready", Assert.Throws<DomainException>(() => shipment.Book(10m, "USD", Now)).Error.Code);

        Assert.Equal("ops.shipments.parcel_count_invalid", Assert.Throws<DomainException>(() => shipment.DeclareParcelCount(0, 0, Now)).Error.Code);
        Assert.Equal("ops.shipments.parcel_count_invalid", Assert.Throws<DomainException>(() => shipment.DeclareParcelCount(100, 0, Now)).Error.Code);
        Assert.Equal("ops.shipments.parcel_count_below", Assert.Throws<DomainException>(() => shipment.DeclareParcelCount(2, 3, Now)).Error.Code);
        shipment.DeclareParcelCount(3, 3, Now);
        Assert.Equal(3, shipment.ParcelCount);
        Assert.Equal("ops.shipments.not_ready", Assert.Throws<DomainException>(() => shipment.Book(10m, "USD", Now)).Error.Code);

        shipment.ChangeInstructions(Home("Ana Pierre", "+50937123456").ForService("air_standard"), Now);
        shipment.Book(42.456m, "USD", Now);
        Assert.Equal((OperationsShipmentStatus.Booked, 42.46m, "USD", Now), (shipment.Status, shipment.BookedTotal!.Value, shipment.BookedCurrencyCode, shipment.BookedUtc!.Value));
        Assert.True(shipment.IsBooked);

        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => shipment.DeclareParcelCount(4, 3, Now)).Error.Code);
        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => shipment.ChangeInstructions(Home("Marie", "+50937123457"), Now)).Error.Code);
        Assert.Equal("ops.receiving.shipment_not_open", Assert.Throws<DomainException>(() => shipment.Cancel(Now)).Error.Code);
    }

    // Slice 5D: the shipment keeps the product chosen and who recorded the customer's acceptance; insuring needs the acceptance, none clears everything.
    [Fact]
    public void Insurance_KeepsTheProductChosen_NeedsTheCustomersAcceptance_AndNoneClearsIt()
    {
        Guid product = Guid.NewGuid();
        Guid clerk = Guid.NewGuid();
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Customer, Warehouse,
            OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), "Ana Pierre", "+50937123456",
                OperationsShipmentOptions.Insured).ForService("air_standard").ForInsurance(product).AcceptTerms(clerk), Now);
        Assert.Equal((product, clerk, Now), (shipment.InsurancePlanId!.Value, shipment.InsuranceRecordedByUserId!.Value, shipment.InsuranceAcceptedUtc!.Value));

        Assert.Equal("ops.shipments.insurance_terms_required", Assert.Throws<DomainException>(() => shipment.ChangeInsurance(Guid.NewGuid(), false, clerk, Now)).Error.Code);
        shipment.ChangeInsurance(null, false, clerk, Now.AddMinutes(1));
        Assert.Equal((false, (Guid?)null, (DateTimeOffset?)null, (Guid?)null),
            (shipment.Options.HasFlag(OperationsShipmentOptions.Insured), shipment.InsurancePlanId, shipment.InsuranceAcceptedUtc, shipment.InsuranceRecordedByUserId));

        // Accepted on the portal: no clerk.
        shipment.ChangeInstructions(OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), "Ana Pierre",
            "+50937123456", OperationsShipmentOptions.Insured).ForService("air_standard").ForInsurance(product).AcceptTerms(), Now.AddMinutes(2));
        Assert.Equal((product, (Guid?)null), (shipment.InsurancePlanId!.Value, shipment.InsuranceRecordedByUserId));
    }

    // Review of 5D: a signature holds for what the customer signed: the same instructions keep it; a parcel, another service or insurance clear it; never once booked.
    [Fact]
    public void Signature_HoldsForWhatWasSigned_AndOnlyWhileOpen()
    {
        Guid clerk = Guid.NewGuid();
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Ana Pierre", "+50937123456").ForService("air_standard"), Now);

        shipment.RecordSignature("signatures/a.png", clerk, Now);
        shipment.ChangeInstructions(Home("Ana Pierre", "+50937123456").ForService("air_standard"), Now.AddMinutes(1));
        Assert.Equal(("signatures/a.png", (DateTimeOffset?)Now, (Guid?)clerk), (shipment.CustomerSignatureKey, shipment.CustomerSignedUtc, shipment.CustomerSignatureRecordedByUserId));

        shipment.TakePackage(Now.AddMinutes(2));
        Assert.Null(shipment.CustomerSignatureKey);

        shipment.RecordSignature("signatures/b.png", clerk, Now.AddMinutes(3));
        shipment.ChangeService("ocean_standard", Now.AddMinutes(4));
        Assert.Null(shipment.CustomerSignedUtc);

        shipment.RecordSignature("signatures/c.png", clerk, Now.AddMinutes(5));
        shipment.ChangeInsurance(Guid.NewGuid(), true, clerk, Now.AddMinutes(6));
        Assert.Null(shipment.CustomerSignatureRecordedByUserId);

        shipment.RecordSignature("signatures/d.png", clerk, Now.AddMinutes(7));
        shipment.DeclareParcelCount(1, 0, Now.AddMinutes(8));
        shipment.Book(10m, "USD", Now.AddMinutes(9));
        Assert.Throws<DomainException>(() => shipment.RecordSignature("signatures/e.png", clerk, Now.AddMinutes(10)));
        Assert.Equal("signatures/d.png", shipment.CustomerSignatureKey);
    }

    private static OperationsShipmentInstructions Home(string name, string phone) =>
        OperationsShipmentInstructions.HomeDelivery(OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"), name, phone, OperationsShipmentOptions.None);

    private static OperationsBranch Branch(OperationsBranchSide side) =>
        OperationsBranch.Create(Tenant, new OperationsLocationDetails("PAP", "Port-au-Prince office", OperationsPostalAddress.Create("12 Rue Capois", null, "Port-au-Prince", null, null, "HT"),
            null, null, null, true), side, Now);

    // Slice 5A: staff change the service of an open shipment that says where it goes; never the waiting one or a cancelled one.
    [Fact]
    public void Service_ChangesOnAnOpenShipmentWithInstructions_Only()
    {
        OperationsShipment shipment = OperationsShipment.Request(Tenant, Customer, Warehouse, Home("Ana Pierre", "+50937123456"), Now);
        shipment.ChangeService(" ground ", Now);
        Assert.Equal("ground", shipment.ServiceCode);

        Assert.Equal("ops.shipments.waiting", Assert.Throws<DomainException>(() => OperationsShipment.Open(Tenant, Customer, Warehouse, Now).ChangeService("ground", Now)).Error.Code);
        shipment.Cancel(Now);
        Assert.Throws<DomainException>(() => shipment.ChangeService("air_standard", Now));
    }
}
