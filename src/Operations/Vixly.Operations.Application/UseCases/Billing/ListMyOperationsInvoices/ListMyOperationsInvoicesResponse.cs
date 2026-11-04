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

    // Slice 5F step 4: the customer may pay by card on Stripe's page.
    public bool CardOffered { get; private init; }

    // Zelle and bank transfer on the portal, when the company offers them there (owner, 2026-10-27).
    public bool ZelleOffered { get; private init; }

    public bool BankTransferOffered { get; private init; }

    public static ListMyOperationsInvoicesResponse Success(IReadOnlyList<OperationsInvoice> invoices, IReadOnlyList<OperationsCreditNote> creditNotes, bool cardOffered = false,
        bool zelleOffered = false, bool bankTransferOffered = false)
    {
        return new ListMyOperationsInvoicesResponse
        {
            Succeeded = true, Invoices = invoices, CreditNotes = creditNotes, CardOffered = cardOffered, ZelleOffered = zelleOffered, BankTransferOffered = bankTransferOffered
        };
    }

    public static ListMyOperationsInvoicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListMyOperationsInvoicesResponse { Error = error };
    }
}
