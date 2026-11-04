using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Arrivals;

// =========================================================
// FILE: OperationsArrivalDiscrepancy.cs
//
// PURPOSE:
// A bundle or parcel that did not match its dispatch at a destination (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Record what was missing or extra, where and on which dispatch
// - Be found by a later scan, or resolved by staff as lost (the customer is told) or otherwise, with a note
//
// IMPORTANT NOTES:
// - The origin and the destination both see it
// - One open discrepancy per bundle or parcel and dispatch
// =========================================================
public sealed class OperationsArrivalDiscrepancy : OperationsEntity
{
    public const int NoteMaxLength = 500;

    public const int CodeMaxLength = 64;

    private OperationsArrivalDiscrepancy()
    {
        Code = string.Empty;
    }

    private OperationsArrivalDiscrepancy(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Code = string.Empty;
    }

    public Guid? DispatchId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public OperationsDiscrepancyKind Kind { get; private set; }

    public Guid? BundleId { get; private set; }

    public Guid? ParcelId { get; private set; }

    // The number scanned or expected.
    public string Code { get; private set; }

    public OperationsDiscrepancyStatus Status { get; private set; }

    public string? Note { get; private set; }

    public Guid? ResolvedByUserId { get; private set; }

    public DateTimeOffset? ResolvedUtc { get; private set; }

    public bool IsOpen => Status == OperationsDiscrepancyStatus.Open;

    public static OperationsArrivalDiscrepancy Create(Guid tenantId, Guid? dispatchId, Guid warehouseId, OperationsDiscrepancyKind kind, Guid? bundleId, Guid? parcelId, string code,
        DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new OperationsArrivalDiscrepancy(tenantId, utcNow)
        {
            DispatchId = dispatchId,
            WarehouseId = warehouseId,
            Kind = kind,
            BundleId = bundleId,
            ParcelId = parcelId,
            Code = code.Length <= CodeMaxLength ? code : code[..CodeMaxLength],
            Status = OperationsDiscrepancyStatus.Open
        };
    }

    // Scanned after all: closed by itself.
    public void Found(DateTimeOffset utcNow)
    {
        if (IsOpen)
        {
            (Status, ResolvedUtc) = (OperationsDiscrepancyStatus.Found, utcNow);
            MarkAsModified(utcNow);
        }
    }

    public void Resolve(OperationsDiscrepancyStatus outcome, string? note, Guid userId, DateTimeOffset utcNow)
    {
        if (!IsOpen)
        {
            throw new DomainException(OperationsArrivalErrors.NotOpen);
        }

        if (outcome is not (OperationsDiscrepancyStatus.Lost or OperationsDiscrepancyStatus.Resolved))
        {
            throw new DomainException(OperationsArrivalErrors.ReasonRequired);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (text is null || text.Length > NoteMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsArrivalErrors.ReasonRequired);
        }

        (Status, Note, ResolvedByUserId, ResolvedUtc) = (outcome, text, userId, utcNow);
        MarkAsModified(utcNow);
    }
}
