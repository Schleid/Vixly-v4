using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.BillOperationsAccount;

// =========================================================
// FILE: BillOperationsAccountResponse.cs
//
// PURPOSE:
// The cashier bills an invoice to the customer's credit account (Slice 5F step 2b, K-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BillOperationsAccountResponse
{
    private BillOperationsAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Awaiting { get; private init; }

    public static BillOperationsAccountResponse Success(bool awaiting)
    {
        return new BillOperationsAccountResponse { Succeeded = true, Awaiting = awaiting };
    }

    public static BillOperationsAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BillOperationsAccountResponse { Error = error };
    }
}
