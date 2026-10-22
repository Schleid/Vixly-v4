using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsParcelPhoto.cs
//
// PURPOSE:
// A photo of a parcel (ADR-0022: label, package, damage, contents).
//
// RESPONSIBILITIES:
// - Point at the file in the tenant's file store; never hold the bytes
//
// IMPORTANT NOTES:
// - Kinds come from the tracking catalogue (OperationsTrackingCodes.PhotoKinds)
// =========================================================
public sealed class OperationsParcelPhoto : OperationsEntity
{
    private OperationsParcelPhoto()
    {
        Kind = string.Empty;
        StorageKey = string.Empty;
        ContentType = string.Empty;
        Sha256 = string.Empty;
    }

    private OperationsParcelPhoto(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Kind = string.Empty;
        StorageKey = string.Empty;
        ContentType = string.Empty;
        Sha256 = string.Empty;
    }

    public Guid ParcelId { get; private set; }

    public string Kind { get; private set; }

    public string StorageKey { get; private set; }

    public string ContentType { get; private set; }

    public int SizeBytes { get; private set; }

    public string Sha256 { get; private set; }

    public Guid? TakenByUserId { get; private set; }

    public static OperationsParcelPhoto Add(Guid tenantId, Guid parcelId, string kind, string storageKey, string contentType, int sizeBytes, string sha256, Guid? takenBy, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { ParcelId = parcelId, Kind = kind, StorageKey = storageKey, ContentType = contentType, SizeBytes = sizeBytes, Sha256 = sha256, TakenByUserId = takenBy };
}
