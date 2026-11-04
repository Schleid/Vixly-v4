using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsUndeclaredDomainTests.cs
//
// PURPOSE:
// Proves the rules of undeclared parcels (Slice 4D step 3, design section 6).
//
// RESPONSIBILITIES:
// - The storage fee after the free days; the company's free days and fee
// - What the label shows, recorded within limits
// - A count: marked, closed only when complete, a missing parcel with a note and by a manager
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsUndeclaredDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StorageFee_IsOwedForEachDayPastTheFreeDays_OnlyWhenTheCompanySetsOne()
    {
        DateTimeOffset received = Now.AddDays(-40).AddHours(-3);

        Assert.Equal(10, OperationsStorageFees.DaysCharged(received, Now, 30));
        Assert.Equal(15.00m, OperationsStorageFees.Accrued(received, Now, 30, 1.50m));
        Assert.Equal(0m, OperationsStorageFees.Accrued(received, Now, 30, null));
        Assert.Equal(0m, OperationsStorageFees.Accrued(Now.AddDays(-12), Now, 30, 1.50m));

        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal((30, (decimal?)null), (settings.StorageFreeDays, settings.StorageFeePerDay));
        settings.ChangeStorageFee(14, 0.75m, Now);
        Assert.Equal((14, (decimal?)0.75m), (settings.StorageFreeDays, settings.StorageFeePerDay));
        settings.ChangeStorageFee(14, 0m, Now);
        Assert.Null(settings.StorageFeePerDay);
        Assert.Equal("ops.holds.storage_fee_invalid", Assert.Throws<DomainException>(() => settings.ChangeStorageFee(-1, null, Now)).Error.Code);
        Assert.Equal("ops.holds.storage_fee_invalid", Assert.Throws<DomainException>(() => settings.ChangeStorageFee(10, 1.005m, Now)).Error.Code);
    }

    [Fact]
    public void Label_IsRecordedWithinLimits_TheCodeInCapitals()
    {
        OperationsParcel parcel = OperationsParcel.Receive(Tenant, "PK26000001", OperationsInboundDelivery.Open(Tenant, Guid.NewGuid(), "UPS", Guid.NewGuid(), "key", Now), "UPS",
            "1Z999AA10123456784", false, Now);

        parcel.RecordLabel(" Ana Pierre ", "c31519", "Shoe store", Now);
        Assert.Equal(("Ana Pierre", "C31519", "Shoe store"), (parcel.LabelName, parcel.LabelCode, parcel.LabelSender));
        parcel.RecordLabel(null, " ", null, Now);
        Assert.Equal(((string?)null, (string?)null), (parcel.LabelName, parcel.LabelCode));
        Assert.Equal("ops.holds.label_text_invalid", Assert.Throws<DomainException>(() => parcel.RecordLabel("<b>Ana</b>", null, null, Now)).Error.Code);
        Assert.Equal("ops.holds.label_text_invalid", Assert.Throws<DomainException>(() => parcel.RecordLabel(null, new string('A', 21), null, Now)).Error.Code);
    }

    [Fact]
    public void Count_ClosesWhenEveryParcelIsMarked_AMissingOneWithANoteAndByAManager()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        OperationsStockCount count = OperationsStockCount.Start(Tenant, Guid.NewGuid(), Guid.NewGuid(), new[] { (first, "PK26000001", (string?)"U-01"), (second, "PK26000002", (string?)null) }, Now);
        Assert.True(count.IsOpen);
        Assert.Equal(2, count.Lines.Count);

        count.Mark(first, OperationsCountResult.Found, null, Now);
        Assert.Equal("ops.holds.count_incomplete", Assert.Throws<DomainException>(() => count.Close(Guid.NewGuid(), true, Now)).Error.Code);
        count.Mark(second, OperationsCountResult.Missing, null, Now);
        Assert.Equal("ops.holds.count_missing_note", Assert.Throws<DomainException>(() => count.Close(Guid.NewGuid(), true, Now)).Error.Code);
        count.Mark(second, OperationsCountResult.Missing, "Not on U-01 or nearby", Now);
        Assert.Equal("ops.holds.approver_not_allowed", Assert.Throws<DomainException>(() => count.Close(Guid.NewGuid(), false, Now)).Error.Code);
        Assert.Equal("ops.holds.count_not_found", Assert.Throws<DomainException>(() => count.Mark(Guid.NewGuid(), OperationsCountResult.Found, null, Now)).Error.Code);

        count.Close(Guid.NewGuid(), true, Now);
        Assert.False(count.IsOpen);
        Assert.Null(count.OpenWarehouseId);
        Assert.Equal("ops.holds.count_closed", Assert.Throws<DomainException>(() => count.Mark(first, OperationsCountResult.Missing, "x", Now)).Error.Code);

        // Every parcel found: any staff member closes it.
        OperationsStockCount clean = OperationsStockCount.Start(Tenant, Guid.NewGuid(), Guid.NewGuid(), new[] { (first, "PK26000001", (string?)null) }, Now);
        clean.Mark(first, OperationsCountResult.Found, null, Now);
        clean.Close(Guid.NewGuid(), false, Now);
        Assert.False(clean.IsOpen);
    }
}
