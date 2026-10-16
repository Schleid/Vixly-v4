using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.ResendPlatformInvoice;

// =========================================================
// FILE: ResendPlatformInvoiceUseCase.cs
//
// PURPOSE:
// Emails an invoice again to the tenant's billing contact (D-B13).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ResendPlatformInvoiceUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ResendPlatformInvoiceUseCase(
        PlatformBillingAdministration billing,
        PlatformOutboxWriter outbox,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ResendPlatformInvoiceResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ResendPlatformInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingRecordPayments, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ResendPlatformInvoiceResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return ResendPlatformInvoiceResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        _outbox.Write(
            PlatformMessageTypes.BillingDocumentIssued,
            invoice.PlatformTenantId,
            new PlatformBillingDocumentIssued(PlatformBillingDocumentKind.Invoice, invoice.Id, invoice.Id, invoice.PlatformTenantId, _billing.UtcNow));
        _billing.Audit(context, "platform.billing.invoice_emailed", PlatformPermissions.BillingRecordPayments, nameof(PlatformInvoice), invoice.Id, new { number = invoice.Number });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ResendPlatformInvoiceResponse.Success()
            : ResendPlatformInvoiceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
