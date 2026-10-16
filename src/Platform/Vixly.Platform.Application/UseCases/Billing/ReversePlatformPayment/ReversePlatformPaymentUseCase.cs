using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.ReversePlatformPayment;

// =========================================================
// FILE: ReversePlatformPaymentUseCase.cs
//
// PURPOSE:
// Reverses a recorded payment (bounced cheque, wrong entry); the balance reopens (D-B7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ReversePlatformPaymentUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ReversePlatformPaymentUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReversePlatformPaymentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReversePlatformPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingRecordPayments, requireStepUp: true, cancellationToken) is { } refused)
        {
            return ReversePlatformPaymentResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return ReversePlatformPaymentResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        try
        {
            PlatformInvoiceEvent reversed = invoice.ReversePayment(request.PaymentId, request.Reason, context.PlatformUserId, _billing.UtcNow);
            _billing.Record(context, invoice, reversed, "platform.billing.payment_reversed", PlatformPermissions.BillingRecordPayments);
        }
        catch (DomainException exception)
        {
            return ReversePlatformPaymentResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ReversePlatformPaymentResponse.Success()
            : ReversePlatformPaymentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
