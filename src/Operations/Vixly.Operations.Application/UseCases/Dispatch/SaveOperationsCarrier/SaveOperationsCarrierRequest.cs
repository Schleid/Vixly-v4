using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsCarrier;

// =========================================================
// FILE: SaveOperationsCarrierRequest.cs
//
// PURPOSE:
// Add a carrier or change one (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsCarrierRequest
{
    // None: a new carrier.
    public Guid? CarrierId { get; init; }

    public string? Name { get; init; }

    public OperationsCarrierKind Kind { get; init; }

    public string? AccountNumber { get; init; }

    public string? AirlinePrefix { get; init; }

    public bool Active { get; init; } = true;
}
