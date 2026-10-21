using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Pickup;

// =========================================================
// FILE: OperationsPickupRelease.cs
//
// PURPOSE:
// A shipment's parcels released at the pickup counter, with the proof taken (Slice 7 step 2, R-5, R-6, R-9).
//
// RESPONSIBILITIES:
// - Who collected, the ID's kind and last characters, the signature, the photo, whether the code matched
// - Who released it, where and when
//
// IMPORTANT NOTES:
// - The full ID number is never kept (R-5)
// - Written once; never changed
// =========================================================
public sealed class OperationsPickupRelease : OperationsEntity
{
    public const int NameMaxLength = 100;

    public const int IdLastLength = 4;

    private OperationsPickupRelease()
    {
        CollectorName = string.Empty;
    }

    private OperationsPickupRelease(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        CollectorName = string.Empty;
    }

    public Guid ShipmentId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public OperationsPickupCollector Collector { get; private set; }

    public string CollectorName { get; private set; }

    public OperationsPickupIdKind? IdKind { get; private set; }

    public string? IdLast { get; private set; }

    public string? SignatureKey { get; private set; }

    public string? PhotoKey { get; private set; }

    public bool CodeChecked { get; private set; }

    public int ParcelCount { get; private set; }

    public Guid ReleasedByUserId { get; private set; }

    public Guid? RequestKey { get; private set; }

    public static OperationsPickupRelease Create(Guid tenantId, Guid shipmentId, Guid warehouseId, OperationsPickupCollector collector, string? collectorName,
        OperationsPickupIdKind? idKind, string? idLast, string? signatureKey, string? photoKey, bool codeChecked, int parcelCount, Guid userId, Guid? requestKey, DateTimeOffset utcNow)
    {
        string name = collectorName?.Trim() ?? string.Empty;

        if (!Enum.IsDefined(collector) || name.Length is 0 or > NameMaxLength || name.Any(char.IsControl) || name.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsPickupErrors.CollectorInvalid);
        }

        string? last = string.IsNullOrWhiteSpace(idLast) ? null : idLast.Trim().ToUpperInvariant();

        if ((idKind is null) != (last is null) || (idKind is { } kind && !Enum.IsDefined(kind))
            || (last is not null && (last.Length != IdLastLength || !last.All(char.IsAsciiLetterOrDigit))))
        {
            throw new DomainException(OperationsPickupErrors.IdRequired);
        }

        if (parcelCount <= 0)
        {
            throw new DomainException(OperationsPickupErrors.NothingReady);
        }

        return new OperationsPickupRelease(tenantId, utcNow)
        {
            ShipmentId = shipmentId,
            WarehouseId = warehouseId,
            Collector = collector,
            CollectorName = name,
            IdKind = idKind,
            IdLast = last,
            SignatureKey = signatureKey,
            PhotoKey = photoKey,
            CodeChecked = codeChecked,
            ParcelCount = parcelCount,
            ReleasedByUserId = userId,
            RequestKey = requestKey
        };
    }
}
