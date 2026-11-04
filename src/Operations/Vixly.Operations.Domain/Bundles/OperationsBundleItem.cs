using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleItem.cs
//
// PURPOSE:
// A parcel or a closed repack box put on a bundle's list from the computer, then confirmed by a scan at the station (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Who put it on the list and when; who scanned it and when
// - Scanned when it goes into the bundle; missing again when it is taken out
//
// IMPORTANT NOTES:
// - A parcel or a box, never both
// - A bundle with a list takes only what is on it, and closes only once everything on it is scanned
// =========================================================
public sealed class OperationsBundleItem : OperationsEntity
{
    private OperationsBundleItem()
    {
    }

    private OperationsBundleItem(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid BundleId { get; private set; }

    public Guid? ParcelId { get; private set; }

    public Guid? BoxId { get; private set; }

    public Guid ShipmentId { get; private set; }

    public Guid AddedByUserId { get; private set; }

    public DateTimeOffset? ScannedUtc { get; private set; }

    public Guid? ScannedByUserId { get; private set; }

    public bool IsScanned => ScannedUtc is not null;

    public static OperationsBundleItem Create(Guid tenantId, Guid bundleId, Guid? parcelId, Guid? boxId, Guid shipmentId, Guid userId, DateTimeOffset utcNow)
    {
        if ((parcelId is null) == (boxId is null))
        {
            throw new ArgumentException("A parcel or a box, never both.", nameof(parcelId));
        }

        return new OperationsBundleItem(tenantId, utcNow) { BundleId = bundleId, ParcelId = parcelId, BoxId = boxId, ShipmentId = shipmentId, AddedByUserId = userId };
    }

    public void Scanned(Guid userId, DateTimeOffset utcNow)
    {
        if (IsScanned)
        {
            return;
        }

        (ScannedUtc, ScannedByUserId) = (utcNow, userId);
        MarkAsModified(utcNow);
    }

    // Taken out of the bundle: missing again, still on the list.
    public void Unscanned(DateTimeOffset utcNow)
    {
        (ScannedUtc, ScannedByUserId) = (null, null);
        MarkAsModified(utcNow);
    }

    public void CheckRemovable()
    {
        if (IsScanned)
        {
            throw new DomainException(OperationsBundleErrors.ItemScanned);
        }
    }
}
