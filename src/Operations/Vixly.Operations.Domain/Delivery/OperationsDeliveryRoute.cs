using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Delivery;

// =========================================================
// FILE: OperationsDeliveryRoute.cs
//
// PURPOSE:
// A driver's day of home deliveries from a warehouse (Slice 7 step 3, R-7, R-12, R-13).
//
// RESPONSIBILITIES:
// - Planned with its stops, out, back when every stop is done, then settled (the cash on delivery paid in at the cashier)
// - Cancelled while planned
//
// IMPORTANT NOTES:
// - A driver who signs in (phone page), or a name only (printed run sheet), as the company records
// - At most 80 stops
// =========================================================
public sealed class OperationsDeliveryRoute : OperationsEntity
{
    public const int MaxStops = 80;

    public const int NameMaxLength = 100;

    public const int NoteMaxLength = 300;

    private OperationsDeliveryRoute()
    {
        Number = string.Empty;
        DriverName = string.Empty;
    }

    private OperationsDeliveryRoute(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = string.Empty;
        DriverName = string.Empty;
    }

    public string Number { get; private set; }

    public Guid WarehouseId { get; private set; }

    public DateOnly ServiceDate { get; private set; }

    public Guid? DriverUserId { get; private set; }

    public string DriverName { get; private set; }

    public OperationsRouteStatus Status { get; private set; }

    public int StopCount { get; private set; }

    public decimal CashCollected { get; private set; }

    public decimal? CashHandedIn { get; private set; }

    public string? SettleNote { get; private set; }

    public string? CancelReason { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid? RequestKey { get; private set; }

    public DateTimeOffset? StartedUtc { get; private set; }

    public DateTimeOffset? ReturnedUtc { get; private set; }

    public DateTimeOffset? SettledUtc { get; private set; }

    public Guid? SettledByUserId { get; private set; }

    public static OperationsDeliveryRoute Plan(Guid tenantId, string number, Guid warehouseId, DateOnly serviceDate, Guid? driverUserId, string? driverName, Guid userId, Guid? requestKey,
        DateOnly today, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        string name = driverName?.Trim() ?? string.Empty;

        if (name.Length is 0 or > NameMaxLength || name.Any(char.IsControl) || name.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsDeliveryErrors.DriverRequired);
        }

        if (serviceDate < today.AddDays(-1) || serviceDate > today.AddDays(30))
        {
            throw new DomainException(OperationsDeliveryErrors.DateInvalid);
        }

        return new OperationsDeliveryRoute(tenantId, utcNow)
        {
            Number = number, WarehouseId = warehouseId, ServiceDate = serviceDate, DriverUserId = driverUserId, DriverName = name, Status = OperationsRouteStatus.Planned,
            CreatedByUserId = userId, RequestKey = requestKey
        };
    }

    public void StopsChanged(int count, DateTimeOffset utcNow)
    {
        if (Status != OperationsRouteStatus.Planned)
        {
            throw new DomainException(OperationsDeliveryErrors.NotPlanned);
        }

        if (count > MaxStops)
        {
            throw new DomainException(OperationsDeliveryErrors.TooManyStops);
        }

        StopCount = count;
        MarkAsModified(utcNow);
    }

    public void Start(DateTimeOffset utcNow)
    {
        if (Status != OperationsRouteStatus.Planned)
        {
            throw new DomainException(OperationsDeliveryErrors.NotPlanned);
        }

        if (StopCount == 0)
        {
            throw new DomainException(OperationsDeliveryErrors.Empty);
        }

        (Status, StartedUtc) = (OperationsRouteStatus.Out, utcNow);
        MarkAsModified(utcNow);
    }

    // A stop done: the cash it brought; back once every stop is done.
    public void StopDone(decimal cash, bool allDone, DateTimeOffset utcNow)
    {
        if (Status != OperationsRouteStatus.Out)
        {
            throw new DomainException(OperationsDeliveryErrors.NotOut);
        }

        CashCollected += cash;

        if (allDone)
        {
            (Status, ReturnedUtc) = (OperationsRouteStatus.Returned, utcNow);
        }

        MarkAsModified(utcNow);
    }

    // The cash counted at the cashier, with a note when it differs (R-13).
    public void Settle(decimal handedIn, string? note, Guid userId, DateTimeOffset utcNow)
    {
        if (Status != OperationsRouteStatus.Returned)
        {
            throw new DomainException(OperationsDeliveryErrors.NotReturned);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (handedIn < 0m || decimal.Round(handedIn, 2) != handedIn || (text is not null && (text.Length > NoteMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0))
            || (handedIn != CashCollected && text is null))
        {
            throw new DomainException(OperationsDeliveryErrors.CashInvalid);
        }

        (Status, CashHandedIn, SettleNote, SettledUtc, SettledByUserId) = (OperationsRouteStatus.Settled, handedIn, text, utcNow, userId);
        MarkAsModified(utcNow);
    }

    public void Cancel(string? reason, DateTimeOffset utcNow)
    {
        if (Status != OperationsRouteStatus.Planned)
        {
            throw new DomainException(OperationsDeliveryErrors.NotPlanned);
        }

        string? text = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (text is null || text.Length > NoteMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0)
        {
            throw new DomainException(OperationsDeliveryErrors.ReasonRequired);
        }

        (Status, CancelReason) = (OperationsRouteStatus.Cancelled, text);
        MarkAsModified(utcNow);
    }
}
