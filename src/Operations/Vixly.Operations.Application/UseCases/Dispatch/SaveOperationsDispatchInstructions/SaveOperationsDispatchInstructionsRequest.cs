using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsDispatchInstructions;

// =========================================================
// FILE: SaveOperationsDispatchInstructionsRequest.cs
//
// PURPOSE:
// What the Shipper's Letter of Instruction adds to the dispatch (Slice 6B step 2, D-6): charges, export reference, documents, handling.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsDispatchInstructionsRequest
{
    public Guid DispatchId { get; init; }

    public bool ChargesCollect { get; init; }

    // The export filing reference (ITN or EEI), when there is one.
    public string? ExportReference { get; init; }

    public OperationsDispatchDocuments Documents { get; init; }

    public string? HandlingInstructions { get; init; }
}
