using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsInvoiceDocument;

// =========================================================
// FILE: GetMyOperationsInvoiceDocumentUseCase.cs
//
// PURPOSE:
// The PDF of one of the customer's own invoices or approved credit notes (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer's own only
// - Printed in the invoice's language
// =========================================================
public sealed class GetMyOperationsInvoiceDocumentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsLabelRenderer _renderer;

    public GetMyOperationsInvoiceDocumentUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsBillingRepository billing,
        IOperationsLabelRenderer renderer)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<GetMyOperationsInvoiceDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsInvoiceDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsInvoiceDocumentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Another customer's invoice answers "not found" (S-3).
        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || invoice.CustomerId != signedIn.Customer.Id)
        {
            return GetMyOperationsInvoiceDocumentResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        OperationsCreditNote? note = null;

        if (request.CreditNoteId is { } noteId
            && (note = (await _billing.ListCreditNotesAsync(invoice.Id, cancellationToken)).FirstOrDefault(item => item.Id == noteId && item.Status == OperationsCreditNoteStatus.Approved)) is null)
        {
            return GetMyOperationsInvoiceDocumentResponse.Failure(OperationsBillingErrors.CreditNotFound);
        }

        return GetMyOperationsInvoiceDocumentResponse.Success(OperationsInvoicePdf.Render(_renderer, new OperationsInvoiceDocument(invoice, note)), (note?.Number ?? invoice.Number) + ".pdf");
    }
}
