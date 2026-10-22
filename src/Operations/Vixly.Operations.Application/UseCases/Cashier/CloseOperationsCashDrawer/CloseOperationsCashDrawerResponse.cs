using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.CloseOperationsCashDrawer;

// =========================================================
// FILE: CloseOperationsCashDrawerResponse.cs
//
// PURPOSE:
// Close the person's cash drawer with the count; the difference is recorded with a note (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CloseOperationsCashDrawerResponse
{
    private CloseOperationsCashDrawerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CloseOperationsCashDrawerResponse Success()
    {
        return new CloseOperationsCashDrawerResponse { Succeeded = true };
    }

    public static CloseOperationsCashDrawerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CloseOperationsCashDrawerResponse { Error = error };
    }
}
