using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.RecordPlatformPayment;

// =========================================================
// FILE: RecordPlatformPaymentUseCase.cs
//
// PURPOSE:
// Records money received by hand against an invoice and issues its receipt (D-B7, D-B11).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RecordPlatformPaymentUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly Vixly.Platform.Application.UseCases.Subscriptions.PlatformSubscriptionAdministration _subscriptions;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RecordPlatformPaymentUseCase(
        PlatformBillingAdministration billing,
        Vixly.Platform.Application.UseCases.Subscriptions.PlatformSubscriptionAdministration subscriptions,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RecordPlatformPaymentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RecordPlatformPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingRecordPayments, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RecordPlatformPaymentResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return RecordPlatformPaymentResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        // Card payments come only from the payment provider (Slice 5), never typed by staff.
        if (request.Method == PlatformPaymentMethod.Card)
        {
            return RecordPlatformPaymentResponse.Failure(PlatformBillingErrors.MethodNotAllowed);
        }

        try
        {
            await new PlatformPaymentApplier(_billing, _subscriptions).ApplyAsync(
                context, invoice, request.Amount, request.Method, request.ReceivedUtc, request.Reference, request.Note, PlatformPermissions.BillingRecordPayments, cancellationToken);
        }
        catch (DomainException exception)
        {
            return RecordPlatformPaymentResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RecordPlatformPaymentResponse.Success()
            : RecordPlatformPaymentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
