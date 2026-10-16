using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.DecidePlatformCreditNote;

// =========================================================
// FILE: DecidePlatformCreditNoteUseCase.cs
//
// PURPOSE:
// The sales manager issues or rejects a requested credit note or void; never the requester (D-B6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class DecidePlatformCreditNoteUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public DecidePlatformCreditNoteUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DecidePlatformCreditNoteResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DecidePlatformCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingApproveCredits, requireStepUp: true, cancellationToken) is { } refused)
        {
            return DecidePlatformCreditNoteResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return DecidePlatformCreditNoteResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        try
        {
            string? number = request.Approve ? await _billing.NextNumberAsync(PlatformBillingDocumentKind.CreditNote, cancellationToken) : null;
            PlatformInvoiceCreditResult decided = invoice.DecideCredit(request.CreditId, request.Approve, number, request.Reason, context.PlatformUserId, _billing.UtcNow);
            string action = !request.Approve ? "platform.billing.credit_rejected" : decided.Credit.IsVoid ? "platform.billing.invoice_voided" : "platform.billing.credit_issued";
            _billing.Record(context, invoice, decided.Event, action, PlatformPermissions.BillingApproveCredits);

            if (request.Approve)
            {
                await _billing.StoreCreditDocumentAsync(invoice, decided.Credit, cancellationToken);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return DecidePlatformCreditNoteResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DecidePlatformCreditNoteResponse.Success()
            : DecidePlatformCreditNoteResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
