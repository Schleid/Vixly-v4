using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsPickupDomainTests.cs
//
// PURPOSE:
// Proves the rules of the pickup counter (Slice 7 step 2, R-4 to R-6, R-10).
//
// RESPONSIBILITIES:
// - A release keeps who collected and only the ID's last four characters
// - The pickup code: six digits, its hash only, checked in constant time
// - The company's pickup settings; storage from the day a parcel is ready
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsPickupDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ARelease_KeepsWhoCollected_AndOnlyTheIdsLastFourCharacters()
    {
        OperationsPickupRelease release = OperationsPickupRelease.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(), OperationsPickupCollector.Named, "  Marie Joseph ", OperationsPickupIdKind.Passport,
            "a1b2", "folder:pickups/x.png", null, true, 2, Guid.NewGuid(), null, Now);
        Assert.Equal(("Marie Joseph", "A1B2", 2, true), (release.CollectorName, release.IdLast, release.ParcelCount, release.CodeChecked));

        Assert.Equal(OperationsPickupErrors.IdRequired, Assert.Throws<DomainException>(() => OperationsPickupRelease.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            OperationsPickupCollector.Recipient, "Jean", OperationsPickupIdKind.NationalId, "123456789", null, null, false, 1, Guid.NewGuid(), null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.IdRequired, Assert.Throws<DomainException>(() => OperationsPickupRelease.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            OperationsPickupCollector.Recipient, "Jean", null, "1234", null, null, false, 1, Guid.NewGuid(), null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.CollectorInvalid, Assert.Throws<DomainException>(() => OperationsPickupRelease.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            OperationsPickupCollector.Recipient, "<b>Jean</b>", null, null, null, null, false, 1, Guid.NewGuid(), null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.CollectorInvalid, Assert.Throws<DomainException>(() => OperationsPickupRelease.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            (OperationsPickupCollector)9, "Jean", null, null, null, null, false, 1, Guid.NewGuid(), null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.NothingReady, Assert.Throws<DomainException>(() => OperationsPickupRelease.Create(Tenant, Guid.NewGuid(), Guid.NewGuid(),
            OperationsPickupCollector.Recipient, "Jean", null, null, null, null, false, 0, Guid.NewGuid(), null, Now)).Error);
    }

    [Fact]
    public void ThePickupCode_IsSixDigits_AndANewCodeStartsTheTriesAgain()
    {
        Assert.Matches("^[0-9]{6}$", OperationsPickupCode.New());
        Vixly.Operations.Domain.Shipments.OperationsShipment shipment = Vixly.Operations.Domain.Shipments.OperationsShipment.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Now);
        shipment.SetPickupCode("protected", Now);

        Assert.False(shipment.PickupCodeLocked);
        shipment.PickupCodeAccepted(Now);
        Assert.Equal(0, shipment.PickupCodeFailures);

        // A person named only once booked, for pickup, until collected.
        Assert.Equal(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.CollectorLocked,
            Assert.Throws<DomainException>(() => shipment.ChangeCollector("Marie", "+50937111111", Now)).Error);
    }

    [Fact]
    public void PickupSettings_AskAnIdAndASignatureByDefault_AndStorageNeedsAFee()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal(OperationsPickupProofs.Id | OperationsPickupProofs.Signature, settings.PickupProofs);
        Assert.False(settings.DestinationCharges || settings.DestinationStorage);
        Assert.Equal(OperationsPickupErrors.SettingsInvalid, Assert.Throws<DomainException>(() => settings.ChangePickup(OperationsPickupProofs.Code, false, true, 3, null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.SettingsInvalid, Assert.Throws<DomainException>(() => settings.ChangePickup(OperationsPickupProofs.Code, false, false, -1, null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.SettingsInvalid, Assert.Throws<DomainException>(() => settings.ChangePickup((OperationsPickupProofs)32, false, false, 3, null, Now)).Error);
        Assert.Equal(OperationsPickupErrors.SettingsInvalid, Assert.Throws<DomainException>(() => settings.ChangePickup(OperationsPickupProofs.Id, false, true, 3, 1.555m, Now)).Error);
        settings.ChangePickup(OperationsPickupProofs.Code | OperationsPickupProofs.Photo, true, true, 3, 1.5m, Now);
        Assert.Equal((OperationsPickupProofs.Code | OperationsPickupProofs.Photo, true, true, 3, 1.5m),
            (settings.PickupProofs, settings.DestinationCharges, settings.DestinationStorage, settings.DestinationStorageFreeDays, settings.DestinationStorageFeePerDay!.Value));
    }

    [Fact]
    public void Storage_RunsFromTheDayAParcelIsReady_WithTheTermsOfThatDay_AndRemindersStepOneAtATime_OnTheShipment()
    {
        OperationsParcel parcel = OperationsParcel.Create(Tenant, Guid.NewGuid(), Now.AddDays(-30));
        parcel.MarkReady(2, 1.5m, Now);
        parcel.MarkReady(0, 9m, Now.AddDays(1));
        Assert.Equal(Now, parcel.ReadyUtc);
        Assert.Equal(0m, parcel.DestinationStorage(Now.AddDays(2)));
        Assert.Equal(4.50m, parcel.DestinationStorage(Now.AddDays(5)));

        // Billed on day 5; a later visit owes only the days since (review).
        parcel.StorageBilled(Now.AddDays(5));
        Assert.Equal(0m, parcel.UnbilledStorage(Now.AddDays(5)));
        Assert.Equal(3m, parcel.UnbilledStorage(Now.AddDays(7)));
        Assert.Null(parcel.StorageFeePerDay);

        OperationsParcel free = OperationsParcel.Create(Tenant, Guid.NewGuid(), Now);
        free.MarkReady(2, null, Now);
        Assert.Equal(0m, free.DestinationStorage(Now.AddDays(30)));

        Vixly.Operations.Domain.Shipments.OperationsShipment waiting = Vixly.Operations.Domain.Shipments.OperationsShipment.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Now);
        waiting.MarkPickupReady(Now);
        waiting.MarkPickupReady(Now.AddDays(3));
        Assert.Equal(Now, waiting.PickupReadyUtc);
        Assert.False(waiting.PickupRemind(2, Now));
        Assert.True(waiting.PickupRemind(1, Now));
        Assert.False(waiting.PickupRemind(1, Now));
    }
}
