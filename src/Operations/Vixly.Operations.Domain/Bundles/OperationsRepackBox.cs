using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsRepackBox.cs
//
// PURPOSE:
// A consolidated shipment's parcels repacked into one box (Slice 6A, D-4; owner, A-3): numbered, weighed, measured and labelled, then put in a bundle.
//
// RESPONSIBILITIES:
// - Take its shipment's parcels in while open
// - Close with the box's weight and size once every parcel is in
// - Go into a bundle and back out if that bundle is voided; be voided while in none
//
// IMPORTANT NOTES:
// - One box per repacked shipment at a time; the use cases check it
// - Its parcels go into the bundle with it
// =========================================================
public sealed class OperationsRepackBox : OperationsEntity
{
    private OperationsRepackBox()
    {
        Number = string.Empty;
    }

    private OperationsRepackBox(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = string.Empty;
    }

    public string Number { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ShipmentId { get; private set; }

    public OperationsRepackBoxStatus Status { get; private set; }

    public int ParcelCount { get; private set; }

    public decimal ParcelsWeightKg { get; private set; }

    public decimal? WeightKg { get; private set; }

    public decimal? LengthCm { get; private set; }

    public decimal? WidthCm { get; private set; }

    public decimal? HeightCm { get; private set; }

    public Guid? BundleId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid? RequestKey { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public Guid? ClosedByUserId { get; private set; }

    public DateTimeOffset? VoidedUtc { get; private set; }

    public Guid? VoidedByUserId { get; private set; }

    public string? VoidReason { get; private set; }

    public static OperationsRepackBox Open(Guid tenantId, string number, OperationsShipment shipment, Guid userId, Guid? requestKey, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentNullException.ThrowIfNull(shipment);

        if (!shipment.IsBooked || !shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate))
        {
            throw new DomainException(OperationsBundleErrors.NotConsolidated);
        }

        return new OperationsRepackBox(tenantId, utcNow)
        {
            Number = number,
            WarehouseId = shipment.OriginWarehouseId,
            ShipmentId = shipment.Id,
            Status = OperationsRepackBoxStatus.Open,
            CreatedByUserId = userId,
            RequestKey = requestKey
        };
    }

    public void Take(decimal weightKg, DateTimeOffset utcNow)
    {
        EnsureOpen();
        (ParcelCount, ParcelsWeightKg) = (ParcelCount + 1, ParcelsWeightKg + Math.Max(0m, weightKg));
        MarkAsModified(utcNow);
    }

    public void Release(decimal weightKg, DateTimeOffset utcNow)
    {
        EnsureOpen();
        (ParcelCount, ParcelsWeightKg) = (Math.Max(0, ParcelCount - 1), Math.Max(0m, ParcelsWeightKg - weightKg));
        MarkAsModified(utcNow);
    }

    // Every parcel of its shipment is in (they are repacked together), then weighed and measured.
    public void Close(OperationsMeasurements measurements, int shipmentParcels, Guid userId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        EnsureOpen();

        if (ParcelCount == 0 || ParcelCount != shipmentParcels)
        {
            throw new DomainException(OperationsBundleErrors.BoxIncomplete);
        }

        (WeightKg, LengthCm, WidthCm, HeightCm) = (measurements.WeightKg, measurements.LengthCm, measurements.WidthCm, measurements.HeightCm);
        (Status, ClosedUtc, ClosedByUserId) = (OperationsRepackBoxStatus.Closed, utcNow, userId);
        MarkAsModified(utcNow);
    }

    public void PutInto(Guid bundleId, DateTimeOffset utcNow)
    {
        if (Status != OperationsRepackBoxStatus.Closed)
        {
            throw new DomainException(OperationsBundleErrors.BoxNotClosed);
        }

        if (BundleId is not null)
        {
            throw new DomainException(OperationsBundleErrors.InAnotherBundle);
        }

        BundleId = bundleId;
        MarkAsModified(utcNow);
    }

    // Out of its bundle: a wrong scan taken out, or the bundle voided.
    public void TakeOut(DateTimeOffset utcNow)
    {
        BundleId = null;
        MarkAsModified(utcNow);
    }

    // A box in no bundle, open or closed, with a reason; its parcels are loose again.
    public void Void(string? reason, Guid userId, DateTimeOffset utcNow)
    {
        string? why = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (why is null || why.Length > OperationsBundleLimits.ReasonMaxLength || why.Any(char.IsControl))
        {
            throw new DomainException(OperationsBundleErrors.ReasonRequired);
        }

        if (Status == OperationsRepackBoxStatus.Voided || BundleId is not null)
        {
            throw new DomainException(OperationsBundleErrors.CannotVoid);
        }

        (Status, VoidedUtc, VoidedByUserId, VoidReason) = (OperationsRepackBoxStatus.Voided, utcNow, userId, why);
        MarkAsModified(utcNow);
    }

    private void EnsureOpen()
    {
        if (Status != OperationsRepackBoxStatus.Open)
        {
            throw new DomainException(OperationsBundleErrors.BoxNotOpen);
        }
    }
}
