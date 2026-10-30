using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.UseCases.Dispatch.AddOperationsDispatchLeg;

// =========================================================
// FILE: AddOperationsDispatchLegRequest.cs
//
// PURPOSE:
// Add a leg to a dispatch before it leaves (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddOperationsDispatchLegRequest
{
    public Guid DispatchId { get; init; }

    public OperationsCarrierKind Kind { get; init; }

    public Guid? CarrierId { get; init; }

    public string? From { get; init; }

    public string? To { get; init; }

    // Scheduled, where the company works.
    public DateTime? Departure { get; init; }

    public DateTime? Arrival { get; init; }

    // The air waybill, bill of lading, truck or tracking number.
    public string? Reference { get; init; }

    public string? Driver { get; init; }

    public string? Vehicle { get; init; }

    public Guid? RequestKey { get; init; }
}
