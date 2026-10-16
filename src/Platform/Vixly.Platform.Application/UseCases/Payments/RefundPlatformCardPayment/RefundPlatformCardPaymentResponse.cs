using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.RefundPlatformCardPayment;

// =========================================================
// FILE: RefundPlatformCardPaymentResponse.cs
//
// PURPOSE:
// Refunds money to the card against a credit note the sales manager approved (D-P6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RefundPlatformCardPaymentResponse
{
    private RefundPlatformCardPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RefundPlatformCardPaymentResponse Success()
    {
        return new RefundPlatformCardPaymentResponse { Succeeded = true };
    }

    public static RefundPlatformCardPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RefundPlatformCardPaymentResponse { Error = error };
    }
}
