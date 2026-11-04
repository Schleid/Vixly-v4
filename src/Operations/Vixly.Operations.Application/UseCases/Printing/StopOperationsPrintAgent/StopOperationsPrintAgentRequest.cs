namespace Vixly.Operations.Application.UseCases.Printing.StopOperationsPrintAgent;

// =========================================================
// FILE: StopOperationsPrintAgentRequest.cs
//
// PURPOSE:
// Stops a dock print program: its key no longer collects labels (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StopOperationsPrintAgentRequest
{
    public Guid WarehouseId { get; init; }

    public Guid AgentId { get; init; }
}
