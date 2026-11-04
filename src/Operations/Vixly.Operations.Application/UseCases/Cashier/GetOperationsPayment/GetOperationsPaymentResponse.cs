using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPayment;

// =========================================================
// FILE: GetOperationsPaymentResponse.cs
//
// PURPOSE:
// A payment just taken (Slice 5F, C-3): its tenders, the change to give and its invoice.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPaymentResponse
{
    private GetOperationsPaymentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsPayment Payment { get; private init; } = null!;

    public OperationsInvoice Invoice { get; private init; } = null!;

    public IReadOnlyDictionary<OperationsPaymentMethod, string> OtherNames { get; private init; } = new Dictionary<OperationsPaymentMethod, string>();

    public static GetOperationsPaymentResponse Success(OperationsPayment payment, OperationsInvoice invoice, IReadOnlyDictionary<OperationsPaymentMethod, string> otherNames)
    {
        return new GetOperationsPaymentResponse { Succeeded = true, Payment = payment, Invoice = invoice, OtherNames = otherNames };
    }

    public static GetOperationsPaymentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPaymentResponse { Error = error };
    }
}
