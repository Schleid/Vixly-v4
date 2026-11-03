using Vixly.Operations.Application.Api;

namespace Vixly.Operations.Application.UseCases.Api.QueryOperationsApi;

// =========================================================
// FILE: QueryOperationsApiRequest.cs
//
// PURPOSE:
// Reads the company’s data for its API (Slice 10): a page of a resource, or one record.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class QueryOperationsApiRequest
{
    public OperationsApiResource Resource { get; init; }

    // One record; none: a page.
    public Guid? Id { get; init; }

    public string? Cursor { get; init; }

    public int Limit { get; init; } = 50;

    public Guid? CustomerId { get; init; }

    public Guid? ShipmentId { get; init; }

    public Guid? SubjectId { get; init; }

    // Tracking of one parcel or one shipment: which of the two the subject is.
    public OperationsApiResource? SubjectResource { get; init; }
}
