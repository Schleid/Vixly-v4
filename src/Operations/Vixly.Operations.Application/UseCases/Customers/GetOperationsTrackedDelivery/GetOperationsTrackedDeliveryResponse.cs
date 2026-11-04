using Vixly.Operations.Application.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsTrackedDelivery;

// =========================================================
// FILE: GetOperationsTrackedDeliveryResponse.cs
//
// PURPOSE:
// What the private link of an "out for delivery" notice shows (Slice 12C, owner H-5), without signing in.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsTrackedDeliveryResponse
{
    private GetOperationsTrackedDeliveryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsDriverWhereabouts? Driver { get; private init; }

    public static GetOperationsTrackedDeliveryResponse Success(OperationsDriverWhereabouts? driver)
    {
        return new GetOperationsTrackedDeliveryResponse { Succeeded = true, Driver = driver };
    }

    public static GetOperationsTrackedDeliveryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsTrackedDeliveryResponse { Error = error };
    }
}
