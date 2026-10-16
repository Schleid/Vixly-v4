using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments.ChargePlatformInvoiceCard;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformInvoiceCardPayments;
using Vixly.Platform.Application.UseCases.Payments.RefundPlatformCardPayment;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: InvoiceCardPaymentsPresenter.cs
//
// PURPOSE:
// Behaviour of the Card payments panel of the invoice page (D-P4, D-P6).
//
// RESPONSIBILITIES:
// - Load card transactions and abilities
// - Charge the saved card for the balance; refund to the card against an approved credit note
//
// IMPORTANT NOTES:
// - Both actions ask for step-up; the payment itself is recorded only when the provider confirms
// =========================================================
public sealed class InvoiceCardPaymentsPresenter : PlatformPresenter<InvoiceCardPaymentsState>
{
    private readonly GetPlatformInvoiceCardPaymentsUseCase _get;

    private readonly ChargePlatformInvoiceCardUseCase _charge;

    private readonly RefundPlatformCardPaymentUseCase _refund;

    public InvoiceCardPaymentsPresenter(GetPlatformInvoiceCardPaymentsUseCase get, ChargePlatformInvoiceCardUseCase charge, RefundPlatformCardPaymentUseCase refund, PlatformUseCaseScope scope, ILogger<InvoiceCardPaymentsPresenter> logger)
        : base(scope, InvoiceCardPaymentsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _charge = charge ?? throw new ArgumentNullException(nameof(charge));
        _refund = refund ?? throw new ArgumentNullException(nameof(refund));
    }

    public async Task LoadAsync(Guid invoiceId)
    {
        GetPlatformInvoiceCardPaymentsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformInvoiceCardPaymentsRequest { PlatformInvoiceId = invoiceId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Payments = response.Payments, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public void Clear()
    {
        SetState(State with { DoneKey = null, Error = null, StepUpPending = false });
    }

    public async Task<bool> ChargeAsync(Guid invoiceId)
    {
        ChargePlatformInvoiceCardResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _charge.ExecuteAsync(context, new ChargePlatformInvoiceCardRequest { PlatformInvoiceId = invoiceId }, cancellationToken);
            return response.Error;
        });

        string? done = response?.Status switch
        {
            PlatformCardTransactionStatus.Failed => null,
            PlatformCardTransactionStatus.RequiresAction => "ui.card_payments.charge_action_required",
            _ => "ui.card_payments.charge_sent"
        };
        Error? failed = error ?? (response?.Status == PlatformCardTransactionStatus.Failed ? Error.Create("ui.card_payments.charge_declined", ("code", response.FailureCode ?? "-")) : null);
        return await FinishAsync(invoiceId, failed, done);
    }

    public async Task<bool> RefundAsync(Guid invoiceId, Guid creditId, decimal amount)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _refund.ExecuteAsync(
            context, new RefundPlatformCardPaymentRequest { PlatformInvoiceId = invoiceId, CreditId = creditId, Amount = amount }, cancellationToken)).Error);
        return await FinishAsync(invoiceId, error, "ui.card_payments.refund_sent");
    }

    private async Task<bool> FinishAsync(Guid invoiceId, Error? error, string? doneKey)
    {
        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        await LoadAsync(invoiceId);
        SetState(State with { StepUpPending = false, DoneKey = error is null ? doneKey : null, Error = error });
        return error is null;
    }
}
