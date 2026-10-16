using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.ReversePlatformPayment;

// =========================================================
// FILE: ReversePlatformPaymentResponse.cs
//
// PURPOSE:
// Reverses a recorded payment (bounced cheque, wrong entry); the balance reopens (D-B7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReversePlatformPaymentResponse
{
    private ReversePlatformPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReversePlatformPaymentResponse Success()
    {
        return new ReversePlatformPaymentResponse { Succeeded = true };
    }

    public static ReversePlatformPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReversePlatformPaymentResponse { Error = error };
    }
}
