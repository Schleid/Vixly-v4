using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Pickup;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsDeliveryStop.cs
//
// PURPOSE:
// One shipment delivered at its address on a route (Slice 7 step 3, R-7, R-13, R-15).
//
// RESPONSIBILITIES:
// - Delivered, with who received it and the proof the company asks, and the cash collected; or failed with a reason
//
// IMPORTANT NOTES:
// - The ID's last 4 characters only, never the full number
// - Done once; never changed
// =========================================================
public sealed class OperationsDeliveryStop : OperationsEntity
{
    public const int NoteMaxLength = 300;

    private OperationsDeliveryStop()
    {
    }

    private OperationsDeliveryStop(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid RouteId { get; private set; }

    public Guid ShipmentId { get; private set; }

    public int Sequence { get; private set; }

    public int ParcelCount { get; private set; }

    public OperationsStopStatus Status { get; private set; }

    public string? ReceiverName { get; private set; }

    public OperationsPickupIdKind? IdKind { get; private set; }

    public string? IdLast { get; private set; }

    public string? SignatureKey { get; private set; }

    public string? PhotoKey { get; private set; }

    public bool CodeChecked { get; private set; }

    // Entered by staff from the printed run sheet: the signature is on the paper (R-12).
    public bool FromSheet { get; private set; }

    public decimal CashCollected { get; private set; }

    // The invoices the cash was collected for (ids, comma separated): owed nothing more at the door until paid in (review).
    public string? CollectedInvoices { get; private set; }

    public IReadOnlyList<Guid> CollectedInvoiceIds => CollectedInvoices is null
        ? Array.Empty<Guid>()
        : CollectedInvoices.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();

    public string? FailureReason { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset? DoneUtc { get; private set; }

    public Guid? DoneByUserId { get; private set; }

    public bool IsDone => Status != OperationsStopStatus.Pending;

    public static OperationsDeliveryStop Create(Guid tenantId, Guid routeId, Guid shipmentId, int sequence, int parcelCount, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { RouteId = routeId, ShipmentId = shipmentId, Sequence = sequence, ParcelCount = parcelCount, Status = OperationsStopStatus.Pending };

    public void Renumber(int sequence, DateTimeOffset utcNow)
    {
        Sequence = sequence;
        MarkAsModified(utcNow);
    }

    public void Deliver(string? receiverName, OperationsPickupIdKind? idKind, string? idLast, string? signatureKey, string? photoKey, bool codeChecked, bool fromSheet, decimal cash,
        Guid userId, DateTimeOffset utcNow, IReadOnlyCollection<Guid>? invoices = null)
    {
        if (IsDone)
        {
            throw new DomainException(OperationsDeliveryErrors.StopDone);
        }

        string name = receiverName?.Trim() ?? string.Empty;

        if (name.Length is 0 or > OperationsPickupRelease.NameMaxLength || name.Any(char.IsControl) || name.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsDeliveryErrors.ReceiverRequired);
        }

        string? last = string.IsNullOrWhiteSpace(idLast) ? null : idLast.Trim().ToUpperInvariant();

        if ((idKind is null) != (last is null) || (idKind is { } kind && !Enum.IsDefined(kind))
            || (last is not null && (last.Length != OperationsPickupRelease.IdLastLength || !last.All(char.IsAsciiLetterOrDigit))))
        {
            throw new DomainException(OperationsPickupErrors.IdRequired);
        }

        if (cash < 0m || cash > 1_000_000m || decimal.Round(cash, 2) != cash)
        {
            throw new DomainException(OperationsDeliveryErrors.CashInvalid);
        }

        (Status, ReceiverName, IdKind, IdLast, SignatureKey, PhotoKey, CodeChecked, FromSheet, CashCollected, DoneUtc, DoneByUserId) =
            (OperationsStopStatus.Delivered, name, idKind, last, signatureKey, photoKey, codeChecked, fromSheet, cash, utcNow, userId);
        CollectedInvoices = cash > 0m && invoices is { Count: > 0 } ? string.Join(",", invoices.Take(10).Select(id => id.ToString("N"))) : null;
        MarkAsModified(utcNow);
    }

    public void Fail(string? reason, string? note, Guid userId, DateTimeOffset utcNow)
    {
        if (IsDone)
        {
            throw new DomainException(OperationsDeliveryErrors.StopDone);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (reason is null || !Vixly.Operations.Domain.Tracking.OperationsTrackingCodes.DeliveryFailures.Contains(reason, StringComparer.Ordinal)
            || (text is not null && (text.Length > NoteMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0)) || (reason == "other" && text is null))
        {
            throw new DomainException(OperationsDeliveryErrors.ReasonRequired);
        }

        (Status, FailureReason, Note, DoneUtc, DoneByUserId) = (OperationsStopStatus.Failed, reason, text, utcNow, userId);
        MarkAsModified(utcNow);
    }
}
