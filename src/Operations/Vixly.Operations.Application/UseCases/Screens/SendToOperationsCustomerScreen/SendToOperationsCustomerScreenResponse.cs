using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.SendToOperationsCustomerScreen;

// =========================================================
// FILE: SendToOperationsCustomerScreenResponse.cs
//
// PURPOSE:
// Sends an invoice to pay or a shipment to sign to a customer screen, or clears it for the next customer (Slice 5F step 3, P-2, P-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendToOperationsCustomerScreenResponse
{
    private SendToOperationsCustomerScreenResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendToOperationsCustomerScreenResponse Success()
    {
        return new SendToOperationsCustomerScreenResponse { Succeeded = true };
    }

    public static SendToOperationsCustomerScreenResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendToOperationsCustomerScreenResponse { Error = error };
    }
}
