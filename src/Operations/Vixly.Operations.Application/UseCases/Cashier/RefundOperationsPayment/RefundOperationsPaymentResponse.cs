using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.RefundOperationsPayment;

// =========================================================
// FILE: RefundOperationsPaymentResponse.cs
//
// PURPOSE:
// Give money back after a credit note on a paid invoice (Slice 5F, F-7 and C-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RefundOperationsPaymentResponse
{
    private RefundOperationsPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RefundId { get; private init; }

    public static RefundOperationsPaymentResponse Success(Guid refundId)
    {
        return new RefundOperationsPaymentResponse { Succeeded = true, RefundId = refundId };
    }

    public static RefundOperationsPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RefundOperationsPaymentResponse { Error = error };
    }
}
