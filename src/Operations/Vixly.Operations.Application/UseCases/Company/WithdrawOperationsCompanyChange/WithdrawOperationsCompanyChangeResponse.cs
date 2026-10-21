using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.WithdrawOperationsCompanyChange;

// =========================================================
// FILE: WithdrawOperationsCompanyChangeResponse.cs
//
// PURPOSE:
// Withdraws the pending request to change the legal details (B12, Owner only).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class WithdrawOperationsCompanyChangeResponse
{
    private WithdrawOperationsCompanyChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static WithdrawOperationsCompanyChangeResponse Success()
    {
        return new WithdrawOperationsCompanyChangeResponse { Succeeded = true };
    }

    public static WithdrawOperationsCompanyChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new WithdrawOperationsCompanyChangeResponse { Error = error };
    }
}
