using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDeliveryStop;

// =========================================================
// FILE: RecordOperationsDeliveryStopResponse.cs
//
// PURPOSE:
// Record how a stop went: delivered with the proof the company asks and the cash collected, or failed with a reason (Slice 7 step 3, R-7, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordOperationsDeliveryStopResponse
{
    private RecordOperationsDeliveryStopResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RecordOperationsDeliveryStopResponse Success()
    {
        return new RecordOperationsDeliveryStopResponse { Succeeded = true };
    }

    public static RecordOperationsDeliveryStopResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordOperationsDeliveryStopResponse { Error = error };
    }
}
