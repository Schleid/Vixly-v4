using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: ShipmentParcelsTests.cs
//
// PURPOSE:
// Proves which parcels still travel with a shipment and which have left the origin (Slice 5A, review).
//
// RESPONSIBILITIES:
// - A parcel disposed of, returned or cancelled is neither on the documents nor "departed"
//
// IMPORTANT NOTES:
// - Pure, no database
// =========================================================
public sealed class ShipmentParcelsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EndedParcels_AreLeftOut_AndNeverCountAsDeparted()
    {
        Guid tenant = Guid.NewGuid();
        OperationsInboundDelivery delivery = OperationsInboundDelivery.Open(tenant, Guid.NewGuid(), "UPS", Guid.NewGuid(), "key-1", Now);
        OperationsParcel Parcel(OperationsMilestone? milestone)
        {
            OperationsParcel parcel = OperationsParcel.Receive(tenant, "PK" + Random.Shared.Next(10000000, 99999999), delivery, "UPS", "1Z999AA1012345678" + Random.Shared.Next(0, 9), false, Now);
            parcel.Project(new OperationsParcelState(milestone, OperationsParcelFlags.None, OperationsHoldReasons.None, Now), Now);
            return parcel;
        }

        OperationsParcel here = Parcel(OperationsMilestone.ReceivedAtOrigin);
        OperationsParcel gone = Parcel(OperationsMilestone.DepartedOrigin);
        OperationsParcel disposed = Parcel(OperationsMilestone.Disposed);
        OperationsParcel returned = Parcel(OperationsMilestone.ReturnedToSender);

        Assert.False(ShipmentParcels.Departed(here));
        Assert.True(ShipmentParcels.Departed(gone));
        Assert.False(ShipmentParcels.Departed(disposed));
        Assert.False(ShipmentParcels.Departed(returned));
        Assert.Equal(new[] { here, gone }, ShipmentParcels.Active(new[] { here, gone, disposed, returned }));
    }
}
