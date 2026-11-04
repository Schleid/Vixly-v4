using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.TransferOperationsOwnership;

// =========================================================
// FILE: TransferOperationsOwnershipResponse.cs
//
// PURPOSE:
// The Owner hands ownership to an active Administrator with a second factor (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TransferOperationsOwnershipResponse
{
    private TransferOperationsOwnershipResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static TransferOperationsOwnershipResponse Success()
    {
        return new TransferOperationsOwnershipResponse { Succeeded = true };
    }

    public static TransferOperationsOwnershipResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TransferOperationsOwnershipResponse { Error = error };
    }
}
