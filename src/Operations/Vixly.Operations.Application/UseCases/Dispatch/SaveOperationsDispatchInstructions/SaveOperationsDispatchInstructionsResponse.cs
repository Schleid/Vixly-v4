using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsDispatchInstructions;

// =========================================================
// FILE: SaveOperationsDispatchInstructionsResponse.cs
//
// PURPOSE:
// What the Shipper's Letter of Instruction adds to the dispatch (Slice 6B step 2, D-6): charges, export reference, documents, handling.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsDispatchInstructionsResponse
{
    private SaveOperationsDispatchInstructionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsDispatchInstructionsResponse Success()
    {
        return new SaveOperationsDispatchInstructionsResponse { Succeeded = true };
    }

    public static SaveOperationsDispatchInstructionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsDispatchInstructionsResponse { Error = error };
    }
}
