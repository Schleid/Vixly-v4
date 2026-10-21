using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.ChangeOperationsCreditAccount;

// =========================================================
// FILE: ChangeOperationsCreditAccountResponse.cs
//
// PURPOSE:
// Change a credit account's limit, terms or state (Slice 5F step 2b, K-1 and K-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsCreditAccountResponse
{
    private ChangeOperationsCreditAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsCreditAccountResponse Success()
    {
        return new ChangeOperationsCreditAccountResponse { Succeeded = true };
    }

    public static ChangeOperationsCreditAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsCreditAccountResponse { Error = error };
    }
}
