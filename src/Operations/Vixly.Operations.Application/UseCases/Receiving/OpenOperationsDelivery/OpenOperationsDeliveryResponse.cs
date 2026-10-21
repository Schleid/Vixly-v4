using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.OpenOperationsDelivery;

// =========================================================
// FILE: OpenOperationsDeliveryResponse.cs
//
// PURPOSE:
// Opens a carrier delivery at the dock: the packages one driver brings (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsDeliveryResponse
{
    private OpenOperationsDeliveryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DeliveryId { get; private init; }

    public static OpenOperationsDeliveryResponse Success(Guid deliveryId)
    {
        return new OpenOperationsDeliveryResponse { Succeeded = true, DeliveryId = deliveryId };
    }

    public static OpenOperationsDeliveryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsDeliveryResponse { Error = error };
    }
}
