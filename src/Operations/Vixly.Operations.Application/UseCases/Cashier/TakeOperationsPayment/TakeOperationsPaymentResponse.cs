using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.TakeOperationsPayment;

// =========================================================
// FILE: TakeOperationsPaymentResponse.cs
//
// PURPOSE:
// Take the payment of an invoice in full, with one or more methods (Slice 5F, F-4, C-3 to C-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TakeOperationsPaymentResponse
{
    private TakeOperationsPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PaymentId { get; private init; }

    public static TakeOperationsPaymentResponse Success(Guid paymentId)
    {
        return new TakeOperationsPaymentResponse { Succeeded = true, PaymentId = paymentId };
    }

    public static TakeOperationsPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TakeOperationsPaymentResponse { Error = error };
    }
}
