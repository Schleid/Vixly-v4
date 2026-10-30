using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.BillOperationsDelivery;

// =========================================================
// FILE: BillOperationsDeliveryResponse.cs
//
// PURPOSE:
// Bill a shipment's destination charges before it goes out for delivery (Slice 7 step 3, R-4, R-9, R-13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BillOperationsDeliveryResponse
{
    private BillOperationsDeliveryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid InvoiceId { get; private init; }

    public static BillOperationsDeliveryResponse Success(Guid invoiceId)
    {
        return new BillOperationsDeliveryResponse { Succeeded = true, InvoiceId = invoiceId };
    }

    public static BillOperationsDeliveryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BillOperationsDeliveryResponse { Error = error };
    }
}
