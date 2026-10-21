using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsStockCount.cs
//
// PURPOSE:
// A count of the undeclared parcels area of a warehouse: what should be on each shelf, and what is (Slice 4D step 3, design section 6).
//
// RESPONSIBILITIES:
// - Start with the undeclared parcels at the warehouse; mark each found or missing; close once every line is marked
// - A parcel missing needs a note, and only the Owner or an administrator closes a count with one missing
//
// IMPORTANT NOTES:
// - One open count per warehouse (a unique index); closed counts never change
// =========================================================
public sealed class OperationsStockCount : OperationsEntity
{
    public const int NoteMaxLength = 300;

    public const int MaxLines = 2000;

    private readonly List<OperationsCountLine> _lines = new();

    private OperationsStockCount()
    {
    }

    private OperationsStockCount(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid WarehouseId { get; private set; }

    public Guid StartedByUserId { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public Guid? ClosedByUserId { get; private set; }

    // Set while the count is open, cleared when it closes: one open count per warehouse.
    public Guid? OpenWarehouseId { get; private set; }

    public IReadOnlyList<OperationsCountLine> Lines => _lines;

    public bool IsOpen => ClosedUtc is null;

    public static OperationsStockCount Start(Guid tenantId, Guid warehouseId, Guid userId, IEnumerable<(Guid ParcelId, string Number, string? Shelf)> parcels, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(parcels);
        OperationsStockCount count = new(tenantId, utcNow) { WarehouseId = warehouseId, OpenWarehouseId = warehouseId, StartedByUserId = userId };
        count._lines.AddRange(parcels.Take(MaxLines).Select(parcel => new OperationsCountLine(parcel.ParcelId, parcel.Number, parcel.Shelf)));
        return count;
    }

    public void Mark(Guid parcelId, OperationsCountResult result, string? note, DateTimeOffset utcNow)
    {
        if (!IsOpen)
        {
            throw new DomainException(OperationsHoldErrors.CountClosed);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (text is { Length: > NoteMaxLength } || (text is not null && text.Any(char.IsControl)) || !Enum.IsDefined(result))
        {
            throw new DomainException(OperationsHoldErrors.TextInvalid);
        }

        OperationsCountLine line = _lines.FirstOrDefault(item => item.ParcelId == parcelId) ?? throw new DomainException(OperationsHoldErrors.CountNotFound);
        line.Mark(result, text);
        MarkAsModified(utcNow);
    }

    // A parcel given to its customer or decided while the count was open is no longer expected in the area.
    public void Resolve(Guid parcelId, DateTimeOffset utcNow)
    {
        if (IsOpen && _lines.FirstOrDefault(item => item.ParcelId == parcelId) is { Result: not OperationsCountResult.Found } line)
        {
            line.Mark(OperationsCountResult.Resolved, null);
            MarkAsModified(utcNow);
        }
    }

    public void Close(Guid userId, bool manager, DateTimeOffset utcNow)
    {
        if (!IsOpen)
        {
            throw new DomainException(OperationsHoldErrors.CountClosed);
        }

        if (_lines.Any(line => line.Result == OperationsCountResult.Expected))
        {
            throw new DomainException(OperationsHoldErrors.CountIncomplete);
        }

        if (_lines.Any(line => line.Result == OperationsCountResult.Missing && line.Note is null))
        {
            throw new DomainException(OperationsHoldErrors.CountMissingNote);
        }

        if (!manager && _lines.Any(line => line.Result == OperationsCountResult.Missing))
        {
            throw new DomainException(OperationsHoldErrors.ApproverNotAllowed);
        }

        (ClosedUtc, ClosedByUserId, OpenWarehouseId) = (utcNow, userId, null);
        MarkAsModified(utcNow);
    }
}
