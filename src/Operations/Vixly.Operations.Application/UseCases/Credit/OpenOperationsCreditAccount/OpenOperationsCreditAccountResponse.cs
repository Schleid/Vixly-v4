using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.OpenOperationsCreditAccount;

// =========================================================
// FILE: OpenOperationsCreditAccountResponse.cs
//
// PURPOSE:
// Open a credit account for a customer (Slice 5F step 2b, K-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsCreditAccountResponse
{
    private OpenOperationsCreditAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CustomerId { get; private init; }

    public static OpenOperationsCreditAccountResponse Success(Guid customerId)
    {
        return new OpenOperationsCreditAccountResponse { Succeeded = true, CustomerId = customerId };
    }

    public static OpenOperationsCreditAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsCreditAccountResponse { Error = error };
    }
}
