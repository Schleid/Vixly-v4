using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Support;

// =========================================================
// FILE: OperationsSupportCase.cs
//
// PURPOSE:
// What a support agent sees about one shipment or package (owner, 2026-10-29, design 9.2).
//
// RESPONSIBILITIES:
// - The status only, for a caller without a matching phone
// - With the caller's phone matched: the milestones, holds, destination, amount due, and the caller's masked contact
//
// IMPORTANT NOTES:
// - Never full addresses, phone or email, identity documents, payment references, notes, costs or other shipments (design 9.2)
// - Other packages of the same shipment show as a count only
// =========================================================
public sealed record OperationsSupportCase(
    bool StatusOnly,
    string Number,
    bool IsPackage,
    IReadOnlyList<OperationsPublicMilestone> Milestones,
    bool OnHold)
{
    public Guid? ShipmentId { get; init; }

    public Guid? ParcelId { get; init; }

    public bool Cancelled { get; init; }

    public bool Collect { get; init; }

    public OperationsHoldReasons Holds { get; init; }

    public string? Destination { get; init; }

    public decimal? AmountDue { get; init; }

    public string? CurrencyCode { get; init; }

    public bool Paid { get; init; }

    // A package by its carrier's number: announced only, or received (and when).
    public bool Received { get; init; }

    public DateTimeOffset? ReceivedUtc { get; init; }

    public int OtherPackages { get; init; }

    public string? CustomerName { get; init; }

    public string? CustomerCode { get; init; }

    public string? MaskedPhone { get; init; }

    public string? MaskedEmail { get; init; }

    public string? City { get; init; }
}
