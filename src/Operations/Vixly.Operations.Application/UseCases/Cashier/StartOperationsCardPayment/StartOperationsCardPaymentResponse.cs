using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.StartOperationsCardPayment;

// =========================================================
// FILE: StartOperationsCardPaymentResponse.cs
//
// PURPOSE:
// Starts a card payment for an invoice at the cashier: Stripe's payment page (QR code or customer screen) or a card reader (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartOperationsCardPaymentResponse
{
    private StartOperationsCardPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ChargeId { get; private init; }

    public static StartOperationsCardPaymentResponse Success(Guid chargeId)
    {
        return new StartOperationsCardPaymentResponse { Succeeded = true, ChargeId = chargeId };
    }

    public static StartOperationsCardPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartOperationsCardPaymentResponse { Error = error };
    }
}
