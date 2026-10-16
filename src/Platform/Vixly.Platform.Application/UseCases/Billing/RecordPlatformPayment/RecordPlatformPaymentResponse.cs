using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.RecordPlatformPayment;

// =========================================================
// FILE: RecordPlatformPaymentResponse.cs
//
// PURPOSE:
// Records money received by hand against an invoice and issues its receipt (D-B7, D-B11).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordPlatformPaymentResponse
{
    private RecordPlatformPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RecordPlatformPaymentResponse Success()
    {
        return new RecordPlatformPaymentResponse { Succeeded = true };
    }

    public static RecordPlatformPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordPlatformPaymentResponse { Error = error };
    }
}
