using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.OpenOperationsCashDrawer;

// =========================================================
// FILE: OpenOperationsCashDrawerResponse.cs
//
// PURPOSE:
// Open the person's cash drawer at a warehouse with a starting float (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsCashDrawerResponse
{
    private OpenOperationsCashDrawerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DrawerId { get; private init; }

    public static OpenOperationsCashDrawerResponse Success(Guid drawerId)
    {
        return new OpenOperationsCashDrawerResponse { Succeeded = true, DrawerId = drawerId };
    }

    public static OpenOperationsCashDrawerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsCashDrawerResponse { Error = error };
    }
}
