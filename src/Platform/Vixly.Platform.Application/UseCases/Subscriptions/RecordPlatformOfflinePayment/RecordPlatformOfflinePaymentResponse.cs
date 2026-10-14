using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RecordPlatformOfflinePayment;

// =========================================================
// FILE: RecordPlatformOfflinePaymentResponse.cs
//
// PURPOSE:
// Records a payment made outside Vixly (bank transfer, cheque) and activates the subscription.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordPlatformOfflinePaymentResponse
{
    private RecordPlatformOfflinePaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RecordPlatformOfflinePaymentResponse Success()
    {
        return new RecordPlatformOfflinePaymentResponse { Succeeded = true };
    }

    public static RecordPlatformOfflinePaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordPlatformOfflinePaymentResponse { Error = error };
    }
}
