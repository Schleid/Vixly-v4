namespace Vixly.Operations.Application.UseCases.Printing.FinishOperationsPrintJob;

// =========================================================
// FILE: FinishOperationsPrintJobRequest.cs
//
// PURPOSE:
// Records what the dock print program did with a label (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FinishOperationsPrintJobRequest
{
    public string AgentKey { get; init; } = string.Empty;

    public Guid JobId { get; init; }

    public bool Printed { get; init; }

    public string? ErrorCode { get; init; }
}
