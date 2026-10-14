using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformSubscriptionNow;
using Vixly.Platform.Application.UseCases.Subscriptions.RecordPlatformOfflinePayment;
using Vixly.Platform.Application.UseCases.Subscriptions.RequestPlatformSubscriptionCancellation;
using Vixly.Platform.Application.UseCases.Subscriptions.ResumePlatformSubscription;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionActionsPresenter.cs
//
// PURPOSE:
// Behaviour of cancellation, resume, ending and offline payment.
//
// RESPONSIBILITIES:
// - Ask for a reason, run the action, ask for step-up where needed and retry
//
// IMPORTANT NOTES:
// - Ending at once and recording a payment need step-up
// =========================================================
public sealed class TenantSubscriptionActionsPresenter : PlatformPresenter<TenantSubscriptionActionState>
{
    private readonly RequestPlatformSubscriptionCancellationUseCase _cancel;

    private readonly ResumePlatformSubscriptionUseCase _resume;

    private readonly EndPlatformSubscriptionNowUseCase _endNow;

    private readonly RecordPlatformOfflinePaymentUseCase _payment;

    public TenantSubscriptionActionsPresenter(
        RequestPlatformSubscriptionCancellationUseCase cancel,
        ResumePlatformSubscriptionUseCase resume,
        EndPlatformSubscriptionNowUseCase endNow,
        RecordPlatformOfflinePaymentUseCase payment,
        PlatformUseCaseScope scope,
        ILogger<TenantSubscriptionActionsPresenter> logger)
        : base(scope, TenantSubscriptionActionState.Initial, logger)
    {
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        _resume = resume ?? throw new ArgumentNullException(nameof(resume));
        _endNow = endNow ?? throw new ArgumentNullException(nameof(endNow));
        _payment = payment ?? throw new ArgumentNullException(nameof(payment));
    }

    public void Start(TenantSubscriptionAction action)
    {
        SetState(new TenantSubscriptionActionState(action, false, null, null));
    }

    public void Cancel()
    {
        SetState(TenantSubscriptionActionState.Initial);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> ResumeAsync(Guid platformTenantId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _resume.ExecuteAsync(
            context, new ResumePlatformSubscriptionRequest { PlatformTenantId = platformTenantId }, cancellationToken)).Error);
        SetState(new TenantSubscriptionActionState(null, false, error is null ? "ui.subscriptions.resumed" : null, error));

        return error is null;
    }

    public async Task<bool> ApplyAsync(Guid platformTenantId, string? reason)
    {
        if (State.Confirming is not TenantSubscriptionAction action)
        {
            return false;
        }

        string why = reason ?? string.Empty;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => action switch
        {
            TenantSubscriptionAction.Cancel => (await _cancel.ExecuteAsync(
                context, new RequestPlatformSubscriptionCancellationRequest { PlatformTenantId = platformTenantId, Reason = why }, cancellationToken)).Error,
            TenantSubscriptionAction.EndNow => (await _endNow.ExecuteAsync(
                context, new EndPlatformSubscriptionNowRequest { PlatformTenantId = platformTenantId, Reason = why }, cancellationToken)).Error,
            _ => (await _payment.ExecuteAsync(
                context, new RecordPlatformOfflinePaymentRequest { PlatformTenantId = platformTenantId, Reason = why }, cancellationToken)).Error
        });

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(error is null
            ? new TenantSubscriptionActionState(null, false, "ui.subscriptions.done." + action.ToString().ToLowerInvariant(), null)
            : State with { StepUpPending = false, Error = error });

        return error is null;
    }
}
