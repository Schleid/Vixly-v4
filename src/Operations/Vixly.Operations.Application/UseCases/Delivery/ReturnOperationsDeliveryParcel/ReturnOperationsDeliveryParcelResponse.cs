using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.ReturnOperationsDeliveryParcel;

// =========================================================
// FILE: ReturnOperationsDeliveryParcelResponse.cs
//
// PURPOSE:
// A parcel that could not be delivered leaves for its origin: returned to the sender (Slice 7 step 3, R-14).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReturnOperationsDeliveryParcelResponse
{
    private ReturnOperationsDeliveryParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReturnOperationsDeliveryParcelResponse Success()
    {
        return new ReturnOperationsDeliveryParcelResponse { Succeeded = true };
    }

    public static ReturnOperationsDeliveryParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReturnOperationsDeliveryParcelResponse { Error = error };
    }
}
