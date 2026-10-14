using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformScheduledChange;
using Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSubscription;
using Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantSubscription;
using Vixly.Platform.Application.UseCases.Subscriptions.PreviewPlatformSubscriptionChange;
using Vixly.Platform.Application.UseCases.Subscriptions.StartPlatformSubscription;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionPresenter.cs
//
// PURPOSE:
// Behaviour of a tenant's subscription section (D-S4, D-S5, D-S15).
//
// RESPONSIBILITIES:
// - Load the subscription
// - Start one for a tenant without one
// - Preview and apply a change; cancel the scheduled change
//
// IMPORTANT NOTES:
// - A change is always previewed first; the same quote is applied
// =========================================================
public sealed class TenantSubscriptionPresenter : PlatformPresenter<TenantSubscriptionState>
{
    private readonly GetPlatformTenantSubscriptionUseCase _get;

    private readonly StartPlatformSubscriptionUseCase _start;

    private readonly PreviewPlatformSubscriptionChangeUseCase _preview;

    private readonly ChangePlatformSubscriptionUseCase _change;

    private readonly CancelPlatformScheduledChangeUseCase _cancelScheduled;

    public TenantSubscriptionPresenter(
        GetPlatformTenantSubscriptionUseCase get,
        StartPlatformSubscriptionUseCase start,
        PreviewPlatformSubscriptionChangeUseCase preview,
        ChangePlatformSubscriptionUseCase change,
        CancelPlatformScheduledChangeUseCase cancelScheduled,
        PlatformUseCaseScope scope,
        ILogger<TenantSubscriptionPresenter> logger)
        : base(scope, TenantSubscriptionState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _start = start ?? throw new ArgumentNullException(nameof(start));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        _change = change ?? throw new ArgumentNullException(nameof(change));
        _cancelScheduled = cancelScheduled ?? throw new ArgumentNullException(nameof(cancelScheduled));
    }

    public async Task LoadAsync(Guid platformTenantId)
    {
        GetPlatformTenantSubscriptionResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(
                context,
                new GetPlatformTenantSubscriptionRequest { PlatformTenantId = platformTenantId, Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName },
                cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with
            {
                Subscription = response.Subscription,
                Abilities = response.Abilities,
                PlanOptions = response.PlanOptions,
                Limits = response.Limits,
                Loaded = true,
                Version = State.Version + 1
            }
            : State with { Loaded = true, Error = error });
    }

    public void Open(TenantSubscriptionMode mode)
    {
        SetState(State with { Mode = mode, Quote = null, DoneKey = null, Error = null });
    }

    public async Task PreviewAsync(Guid platformTenantId, TenantSubscriptionModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        PreviewPlatformSubscriptionChangeResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _preview.ExecuteAsync(
                context,
                new PreviewPlatformSubscriptionChangeRequest { PlatformTenantId = platformTenantId, PlatformPlanId = model.PlatformPlanId, Term = model.Term, Licences = model.Licences },
                cancellationToken);
            return response.Error;
        });

        SetState(State with { Quote = error is null ? response?.Quote : null, Error = error });
    }

    public async Task<bool> SubmitAsync(Guid platformTenantId, TenantSubscriptionModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        bool starting = State.Mode == TenantSubscriptionMode.Start;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => starting
            ? (await _start.ExecuteAsync(
                context,
                new StartPlatformSubscriptionRequest
                {
                    PlatformTenantId = platformTenantId,
                    PlatformPlanId = model.PlatformPlanId,
                    Term = model.Term,
                    Licences = model.Licences,
                    StartTrial = model.StartTrial
                },
                cancellationToken)).Error
            : (await _change.ExecuteAsync(
                context,
                new ChangePlatformSubscriptionRequest { PlatformTenantId = platformTenantId, PlatformPlanId = model.PlatformPlanId, Term = model.Term, Licences = model.Licences },
                cancellationToken)).Error);

        return await FinishAsync(platformTenantId, error, starting ? "ui.subscriptions.started" : "ui.subscriptions.changed");
    }

    public async Task<bool> CancelScheduledAsync(Guid platformTenantId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _cancelScheduled.ExecuteAsync(
            context, new CancelPlatformScheduledChangeRequest { PlatformTenantId = platformTenantId }, cancellationToken)).Error);

        return await FinishAsync(platformTenantId, error, "ui.subscriptions.scheduled_cancelled");
    }

    private async Task<bool> FinishAsync(Guid platformTenantId, Error? error, string doneKey)
    {
        if (error is not null)
        {
            SetState(State with { DoneKey = null, Error = error });
            return false;
        }

        SetState(State with { Mode = TenantSubscriptionMode.None, Quote = null, DoneKey = doneKey, Error = null });
        await LoadAsync(platformTenantId);

        return true;
    }
}
