using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee;

// =========================================================
// FILE: ChangeOperationsStorageFeeResponse.cs
//
// PURPOSE:
// Changes the days a package waits free of charge and the storage fee per day after them (Slice 4D step 3, Q-D3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsStorageFeeResponse
{
    private ChangeOperationsStorageFeeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsStorageFeeResponse Success()
    {
        return new ChangeOperationsStorageFeeResponse { Succeeded = true };
    }

    public static ChangeOperationsStorageFeeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsStorageFeeResponse { Error = error };
    }
}
