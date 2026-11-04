using Vixly.Operations.Domain.Events;

namespace Vixly.Operations.Application.UseCases.Events.SaveOperationsEventReason;

// =========================================================
// FILE: SaveOperationsEventReasonRequest.cs
//
// PURPOSE:
// Add a cause of the company's own, or change one (Slice 6C, E-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsEventReasonRequest
{
    // None: a new cause.
    public Guid? ReasonId { get; init; }

    public OperationsEventKind Kind { get; init; }

    public string? TextEn { get; init; }

    public string? TextFr { get; init; }

    public string? TextEs { get; init; }

    public bool Active { get; init; } = true;
}
