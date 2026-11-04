using Vixly.Operations.Application.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.ListOperationsInvoices;

// =========================================================
// FILE: ListOperationsInvoicesResponse.cs
//
// PURPOSE:
// The invoices of the warehouses a person sees (Slice 5F, I-8): find by number, customer or shipment, filter by state.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsInvoicesResponse
{
    private ListOperationsInvoicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsInvoiceRow> Invoices { get; private init; } = Array.Empty<OperationsInvoiceRow>();

    public int Requests { get; private init; }

    public static ListOperationsInvoicesResponse Success(IReadOnlyList<OperationsInvoiceRow> invoices, int requests)
    {
        return new ListOperationsInvoicesResponse { Succeeded = true, Invoices = invoices, Requests = requests };
    }

    public static ListOperationsInvoicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsInvoicesResponse { Error = error };
    }
}
