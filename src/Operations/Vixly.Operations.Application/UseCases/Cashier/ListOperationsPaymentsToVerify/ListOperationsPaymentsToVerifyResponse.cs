using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ListOperationsPaymentsToVerify;

// =========================================================
// FILE: ListOperationsPaymentsToVerifyResponse.cs
//
// PURPOSE:
// Zelle and bank transfers waiting for finance (Slice 5F, C-6): the invoice, customer, amount, date and confirmation number.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsPaymentsToVerifyResponse
{
    private ListOperationsPaymentsToVerifyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsPayment> Payments { get; private init; } = Array.Empty<OperationsPayment>();

    public IReadOnlyDictionary<Guid, OperationsInvoice> Invoices { get; private init; } = new Dictionary<Guid, OperationsInvoice>();

    public static ListOperationsPaymentsToVerifyResponse Success(IReadOnlyList<OperationsPayment> payments, IReadOnlyDictionary<Guid, OperationsInvoice> invoices)
    {
        return new ListOperationsPaymentsToVerifyResponse { Succeeded = true, Payments = payments, Invoices = invoices };
    }

    public static ListOperationsPaymentsToVerifyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsPaymentsToVerifyResponse { Error = error };
    }
}
