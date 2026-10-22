using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.CheckOperationsPayment;

// =========================================================
// FILE: CheckOperationsPaymentResponse.cs
//
// PURPOSE:
// Finance's check of a Zelle or transfer (Slice 5F, C-6): received settles it; not received takes it back, the invoice is owed again and the customer is told.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckOperationsPaymentResponse
{
    private CheckOperationsPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CheckOperationsPaymentResponse Success()
    {
        return new CheckOperationsPaymentResponse { Succeeded = true };
    }

    public static CheckOperationsPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckOperationsPaymentResponse { Error = error };
    }
}
