using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.CancelOperationsCardPayment;

// =========================================================
// FILE: CancelOperationsCardPaymentResponse.cs
//
// PURPOSE:
// Cancels a card payment that has not been paid (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelOperationsCardPaymentResponse
{
    private CancelOperationsCardPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelOperationsCardPaymentResponse Success()
    {
        return new CancelOperationsCardPaymentResponse { Succeeded = true };
    }

    public static CancelOperationsCardPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelOperationsCardPaymentResponse { Error = error };
    }
}
