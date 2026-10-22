using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.RemoveOperationsCustomerScreen;

// =========================================================
// FILE: RemoveOperationsCustomerScreenResponse.cs
//
// PURPOSE:
// Removes a customer screen; it stops working at once (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsCustomerScreenResponse
{
    private RemoveOperationsCustomerScreenResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsCustomerScreenResponse Success()
    {
        return new RemoveOperationsCustomerScreenResponse { Succeeded = true };
    }

    public static RemoveOperationsCustomerScreenResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsCustomerScreenResponse { Error = error };
    }
}
