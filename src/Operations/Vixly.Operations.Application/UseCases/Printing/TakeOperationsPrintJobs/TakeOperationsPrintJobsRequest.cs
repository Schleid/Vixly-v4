namespace Vixly.Operations.Application.UseCases.Printing.TakeOperationsPrintJobs;

// =========================================================
// FILE: TakeOperationsPrintJobsRequest.cs
//
// PURPOSE:
// Hands waiting labels to a dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TakeOperationsPrintJobsRequest
{
    public string AgentKey { get; init; } = string.Empty;
}
