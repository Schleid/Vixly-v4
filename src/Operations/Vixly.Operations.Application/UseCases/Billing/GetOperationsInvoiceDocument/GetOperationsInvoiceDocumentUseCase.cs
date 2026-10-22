using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using System.Globalization;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceDocument;

// =========================================================
// FILE: GetOperationsInvoiceDocumentUseCase.cs
//
// PURPOSE:
// The PDF of an invoice or of one of its approved credit notes (Slice 5F, I-2 and I-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.invoices.view or ops.counter.serve, at the invoice's warehouse
// - Printed in the invoice's language
// =========================================================
public sealed class GetOperationsInvoiceDocumentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsLabelRenderer _renderer;

    public GetOperationsInvoiceDocumentUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsLabelRenderer renderer)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<GetOperationsInvoiceDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsInvoiceDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> invoices, Error? _) = await _desk.OpenAsync(context, OperationsPermissions.InvoicesView, cancellationToken);

        if (access is null)
        {
            return GetOperationsInvoiceDocumentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // The counter prints the invoices of its own warehouse too.
        IReadOnlyList<OperationsWarehouse> counters = await _desk.WarehousesAsync(access, OperationsPermissions.CounterServe, cancellationToken);

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice
            || invoices.Concat(counters).All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return GetOperationsInvoiceDocumentResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        OperationsCreditNote? note = null;

        if (request.CreditNoteId is { } noteId
            && (note = (await _billing.ListCreditNotesAsync(invoice.Id, cancellationToken)).FirstOrDefault(item => item.Id == noteId && item.Status == OperationsCreditNoteStatus.Approved)) is null)
        {
            return GetOperationsInvoiceDocumentResponse.Failure(OperationsBillingErrors.CreditNotFound);
        }

        return GetOperationsInvoiceDocumentResponse.Success(OperationsInvoicePdf.Render(_renderer, new OperationsInvoiceDocument(invoice, note)), (note?.Number ?? invoice.Number) + ".pdf");
    }
}
