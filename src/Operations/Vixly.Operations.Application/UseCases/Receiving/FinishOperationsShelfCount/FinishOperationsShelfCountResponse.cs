using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.FinishOperationsShelfCount;

// =========================================================
// FILE: FinishOperationsShelfCountResponse.cs
//
// PURPOSE:
// Finishes a shelf count (Slice 12D, owner H-7): what was found, what is missing, what was recorded elsewhere, and unknown codes.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FinishOperationsShelfCountResponse
{
    private FinishOperationsShelfCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static FinishOperationsShelfCountResponse Success()
    {
        return new FinishOperationsShelfCountResponse { Succeeded = true };
    }

    public static FinishOperationsShelfCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FinishOperationsShelfCountResponse { Error = error };
    }
}
