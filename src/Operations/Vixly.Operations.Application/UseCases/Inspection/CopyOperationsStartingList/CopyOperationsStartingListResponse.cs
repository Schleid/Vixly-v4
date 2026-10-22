using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.CopyOperationsStartingList;

// =========================================================
// FILE: CopyOperationsStartingListResponse.cs
//
// PURPOSE:
// Copies the starting restricted-items list into an empty company list, with a rule that uses it (Slice 3C, D-R5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CopyOperationsStartingListResponse
{
    private CopyOperationsStartingListResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Added { get; private init; }

    public static CopyOperationsStartingListResponse Success(int added)
    {
        return new CopyOperationsStartingListResponse { Succeeded = true, Added = added };
    }

    public static CopyOperationsStartingListResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CopyOperationsStartingListResponse { Error = error };
    }
}
