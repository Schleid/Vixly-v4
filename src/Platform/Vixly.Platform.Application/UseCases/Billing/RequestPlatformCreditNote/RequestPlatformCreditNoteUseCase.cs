using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.RequestPlatformCreditNote;

// =========================================================
// FILE: RequestPlatformCreditNoteUseCase.cs
//
// PURPOSE:
// Asks the sales manager to approve a credit note or a void (D-B6).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RequestPlatformCreditNoteUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RequestPlatformCreditNoteUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RequestPlatformCreditNoteResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RequestPlatformCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingRequestCredits, requireStepUp: false, cancellationToken) is { } refused)
        {
            return RequestPlatformCreditNoteResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return RequestPlatformCreditNoteResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        try
        {
            PlatformInvoiceEvent requested = invoice.RequestCredit(request.Amount, request.Void, request.Reason, context.PlatformUserId, _billing.UtcNow);
            _billing.Record(context, invoice, requested, request.Void ? "platform.billing.void_requested" : "platform.billing.credit_requested", PlatformPermissions.BillingRequestCredits);
            _billing.RequestApproval(invoice, context.PlatformUserId);
        }
        catch (DomainException exception)
        {
            return RequestPlatformCreditNoteResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RequestPlatformCreditNoteResponse.Success()
            : RequestPlatformCreditNoteResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
