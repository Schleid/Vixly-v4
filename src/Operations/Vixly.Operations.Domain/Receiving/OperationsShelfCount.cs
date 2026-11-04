using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsShelfCount.cs
//
// PURPOSE:
// A basic count of one shelf (Slice 12D, owner H-7): scan the shelf, scan every parcel on it, then see what is found, missing and extra.
//
// RESPONSIBILITIES:
// - Keep the codes scanned, once each, while the count is open
// - Keep the result once finished: found, missing, extra (recorded elsewhere) and unknown codes
//
// IMPORTANT NOTES:
// - At most 500 scans; a finished count never changes
// - The lists are parcel numbers (or codes as scanned, for unknown ones), one per line
// - Full counts of a warehouse and the exceptions queue are the warehouse management add-on, not this
// - Bound to its tenant by OperationsEntity; guarded by a row version (two scanners at once)
// =========================================================
public sealed class OperationsShelfCount : OperationsEntity
{
    public const int MaxScans = 500;

    public const int CodeMaxLength = 40;

    private OperationsShelfCount()
    {
        LocationCode = string.Empty;
        Scanned = string.Empty;
    }

    private OperationsShelfCount(Guid tenantId, Guid warehouseId, Guid locationId, string locationCode, Guid userId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        WarehouseId = warehouseId;
        LocationId = locationId;
        LocationCode = locationCode;
        StartedByUserId = userId;
        Scanned = string.Empty;
    }

    public Guid WarehouseId { get; private set; }

    public Guid LocationId { get; private set; }

    public string LocationCode { get; private set; }

    public Guid StartedByUserId { get; private set; }

    public string Scanned { get; private set; }

    public DateTimeOffset? FinishedUtc { get; private set; }

    public Guid? FinishedByUserId { get; private set; }

    public string? Found { get; private set; }

    public string? Missing { get; private set; }

    public string? Extra { get; private set; }

    public string? Unknown { get; private set; }

    public bool IsOpen => FinishedUtc is null;

    public IReadOnlyList<string> ScannedCodes => Lines(Scanned);

    public static OperationsShelfCount Start(Guid tenantId, Guid warehouseId, OperationsStorageLocation shelf, Guid userId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(shelf);

        if (shelf.WarehouseId != warehouseId || !shelf.IsActive)
        {
            throw new DomainException(OperationsReceivingErrors.ShelfUnknown);
        }

        return new OperationsShelfCount(tenantId, warehouseId, shelf.Id, shelf.Code, userId, utcNow);
    }

    // True when the code is new to this count; a second scan of it changes nothing.
    public bool Scan(string code, DateTimeOffset utcNow)
    {
        if (!IsOpen)
        {
            throw new DomainException(OperationsReceivingErrors.CountClosed);
        }

        string text = code?.Trim().ToUpperInvariant() ?? string.Empty;

        if (text.Length is 0 or > CodeMaxLength || text.Any(char.IsControl))
        {
            throw new DomainException(OperationsReceivingErrors.TextTooLong);
        }

        IReadOnlyList<string> codes = ScannedCodes;

        if (codes.Contains(text, StringComparer.Ordinal))
        {
            return false;
        }

        if (codes.Count >= MaxScans)
        {
            throw new DomainException(OperationsReceivingErrors.CountFull);
        }

        Scanned = codes.Count == 0 ? text : Scanned + "\n" + text;
        MarkAsModified(utcNow);
        return true;
    }

    public void Finish(IEnumerable<string> found, IEnumerable<string> missing, IEnumerable<string> extra, IEnumerable<string> unknown, Guid userId, DateTimeOffset utcNow)
    {
        if (!IsOpen)
        {
            throw new DomainException(OperationsReceivingErrors.CountClosed);
        }

        (Found, Missing, Extra, Unknown) = (Join(found), Join(missing), Join(extra), Join(unknown));
        (FinishedUtc, FinishedByUserId) = (utcNow, userId);
        MarkAsModified(utcNow);
    }

    public static IReadOnlyList<string> Lines(string? text) =>
        string.IsNullOrEmpty(text) ? Array.Empty<string>() : text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static string Join(IEnumerable<string> items) => string.Join("\n", items ?? Array.Empty<string>());
}
