using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.ListMyOperationsInvoices;

// =========================================================
// FILE: ListMyOperationsInvoicesResponse.cs
//
// PURPOSE:
// The customer's own invoices and their approved credit notes (Slice 5F, I-8), on the portal.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListMyOperationsInvoicesResponse
{
    private ListMyOperationsInvoicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsInvoice> Invoices { get; private init; } = Array.Empty<OperationsInvoice>();

    public IReadOnlyList<OperationsCreditNote> CreditNotes { get; private init; } = Array.Empty<OperationsCreditNote>();

    public static ListMyOperationsInvoicesResponse Success(IReadOnlyList<OperationsInvoice> invoices, IReadOnlyList<OperationsCreditNote> creditNotes)
    {
        return new ListMyOperationsInvoicesResponse { Succeeded = true, Invoices = invoices, CreditNotes = creditNotes };
    }

    public static ListMyOperationsInvoicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListMyOperationsInvoicesResponse { Error = error };
    }
}
