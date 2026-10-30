using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Pickup;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsDeliveryStopRow.cs
//
// PURPOSE:
// A stop of a delivery route: where, for whom, what to collect, and how it went (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Carry the values a page shows
//
// IMPORTANT NOTES:
// - Read model only; the address as the customer gave it
// =========================================================
public sealed record OperationsDeliveryStopRow(Guid Id, int Sequence, Guid ShipmentId, string Number, string? RecipientName, string? RecipientPhone, string AddressLine, int Parcels,
    decimal Due, string? CurrencyCode, OperationsStopStatus Status)
{
    public string? ReceiverName { get; init; }

    public OperationsPickupIdKind? IdKind { get; init; }

    public bool Signed { get; init; }

    public bool Photographed { get; init; }

    public bool CodeChecked { get; init; }

    public bool FromSheet { get; init; }

    public decimal CashCollected { get; init; }

    public string? FailureReason { get; init; }

    public string? Note { get; init; }

    public DateTimeOffset? DoneUtc { get; init; }

    // Unpaid and nothing to collect for it: it cannot be delivered (cash on delivery off, or a payment awaiting confirmation).
    public bool Blocked { get; init; }
}
