using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplies;

// =========================================================
// FILE: ReceiveOperationsSuppliesResponse.cs
//
// PURPOSE:
// Supplies received at a warehouse (Slice 5E, S-5): each line adds stock with its cost; the supplier and the reference are kept.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReceiveOperationsSuppliesResponse
{
    private ReceiveOperationsSuppliesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReceiveOperationsSuppliesResponse Success()
    {
        return new ReceiveOperationsSuppliesResponse { Succeeded = true };
    }

    public static ReceiveOperationsSuppliesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReceiveOperationsSuppliesResponse { Error = error };
    }
}
