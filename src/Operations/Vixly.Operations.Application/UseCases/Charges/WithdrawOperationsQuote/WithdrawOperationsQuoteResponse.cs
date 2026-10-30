using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.WithdrawOperationsQuote;

// =========================================================
// FILE: WithdrawOperationsQuoteResponse.cs
//
// PURPOSE:
// Withdraws a quote: it no longer counts (Slice 4 design section 7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class WithdrawOperationsQuoteResponse
{
    private WithdrawOperationsQuoteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static WithdrawOperationsQuoteResponse Success()
    {
        return new WithdrawOperationsQuoteResponse { Succeeded = true };
    }

    public static WithdrawOperationsQuoteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new WithdrawOperationsQuoteResponse { Error = error };
    }
}
