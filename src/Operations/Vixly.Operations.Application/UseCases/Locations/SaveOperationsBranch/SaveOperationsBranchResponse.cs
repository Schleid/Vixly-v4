using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;

// =========================================================
// FILE: SaveOperationsBranchResponse.cs
//
// PURPOSE:
// Creates or changes a branch (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsBranchResponse
{
    private SaveOperationsBranchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid BranchId { get; private init; }

    public static SaveOperationsBranchResponse Success(Guid branchId)
    {
        return new SaveOperationsBranchResponse { Succeeded = true, BranchId = branchId };
    }

    public static SaveOperationsBranchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsBranchResponse { Error = error };
    }
}
