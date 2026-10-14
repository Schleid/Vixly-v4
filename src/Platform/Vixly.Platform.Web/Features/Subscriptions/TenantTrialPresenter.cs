using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformTrial;
using Vixly.Platform.Application.UseCases.Subscriptions.ExtendPlatformTrial;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantTrialPresenter.cs
//
// PURPOSE:
// Behaviour of the trial actions (owner, D-S12).
//
// RESPONSIBILITIES:
// - Extend a trial to a new date or end it now, with a reason
//
// IMPORTANT NOTES:
// - The owner asked for this flexibility: a trial can run longer than its plan offers
// =========================================================
public sealed class TenantTrialPresenter : PlatformPresenter<TenantSubscriptionActionState>
{
    private readonly ExtendPlatformTrialUseCase _extend;

    private readonly EndPlatformTrialUseCase _end;

    public TenantTrialPresenter(
        ExtendPlatformTrialUseCase extend,
        EndPlatformTrialUseCase end,
        PlatformUseCaseScope scope,
        ILogger<TenantTrialPresenter> logger)
        : base(scope, TenantSubscriptionActionState.Initial, logger)
    {
        _extend = extend ?? throw new ArgumentNullException(nameof(extend));
        _end = end ?? throw new ArgumentNullException(nameof(end));
    }

    public void Start(TenantSubscriptionAction action)
    {
        SetState(new TenantSubscriptionActionState(action, false, null, null));
    }

    public void Cancel()
    {
        SetState(TenantSubscriptionActionState.Initial);
    }

    public async Task<bool> ApplyAsync(Guid platformTenantId, DateTime? newEndDate, string? reason)
    {
        if (State.Confirming is not TenantSubscriptionAction action)
        {
            return false;
        }

        string why = reason ?? string.Empty;
        DateTimeOffset newEnd = newEndDate is DateTime date
            ? new DateTimeOffset(DateTime.SpecifyKind(date.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc))
            : DateTimeOffset.MinValue;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => action == TenantSubscriptionAction.ExtendTrial
            ? (await _extend.ExecuteAsync(context, new ExtendPlatformTrialRequest { PlatformTenantId = platformTenantId, TrialEndsUtc = newEnd, Reason = why }, cancellationToken)).Error
            : (await _end.ExecuteAsync(context, new EndPlatformTrialRequest { PlatformTenantId = platformTenantId, Reason = why }, cancellationToken)).Error);

        SetState(error is null
            ? new TenantSubscriptionActionState(null, false, "ui.subscriptions.done." + action.ToString().ToLowerInvariant(), null)
            : State with { Error = error });

        return error is null;
    }
}
