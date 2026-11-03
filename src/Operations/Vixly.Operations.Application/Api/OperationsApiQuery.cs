namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiQuery.cs
//
// PURPOSE:
// A page of a resource for the company API or a webhook feed (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the place after which to read (time, then id), the limit and the filters
//
// IMPORTANT NOTES:
// - Rows come in order of creation, then id: a cursor never skips or repeats one
// - Before: rows newer than this are left for the next read (they may still be committing)
// =========================================================
public sealed record OperationsApiQuery(DateTimeOffset AfterUtc, Guid AfterId, int Limit)
{
    public DateTimeOffset? BeforeUtc { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? ShipmentId { get; init; }

    // Tracking only: the parcel or shipment whose events are read, and which of the two it is.
    public Guid? SubjectId { get; init; }

    public OperationsApiResource? SubjectResource { get; init; }
}
