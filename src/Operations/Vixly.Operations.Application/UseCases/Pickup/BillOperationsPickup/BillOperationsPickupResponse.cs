using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.BillOperationsPickup;

// =========================================================
// FILE: BillOperationsPickupResponse.cs
//
// PURPOSE:
// Bill a shipment's destination charges and storage at the pickup counter (Slice 7 step 2, R-4, R-10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BillOperationsPickupResponse
{
    private BillOperationsPickupResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid InvoiceId { get; private init; }

    public static BillOperationsPickupResponse Success(Guid invoiceId)
    {
        return new BillOperationsPickupResponse { Succeeded = true, InvoiceId = invoiceId };
    }

    public static BillOperationsPickupResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BillOperationsPickupResponse { Error = error };
    }
}
