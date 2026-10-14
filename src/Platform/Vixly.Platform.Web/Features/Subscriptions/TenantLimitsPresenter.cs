using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions.RemovePlatformTenantLimitOverride;
using Vixly.Platform.Application.UseCases.Subscriptions.SetPlatformTenantLimitOverride;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantLimitsPresenter.cs
//
// PURPOSE:
// Behaviour of the limits section (D-S8).
//
// RESPONSIBILITIES:
// - Set an override with a reason and optional end; remove one
// - Ask for step-up and let the component retry
//
// IMPORTANT NOTES:
// - A new override replaces the open one for the same limit
// =========================================================
public sealed class TenantLimitsPresenter : PlatformPresenter<TenantLimitsState>
{
    private readonly SetPlatformTenantLimitOverrideUseCase _set;

    private readonly RemovePlatformTenantLimitOverrideUseCase _remove;

    public TenantLimitsPresenter(
        SetPlatformTenantLimitOverrideUseCase set,
        RemovePlatformTenantLimitOverrideUseCase remove,
        PlatformUseCaseScope scope,
        ILogger<TenantLimitsPresenter> logger)
        : base(scope, TenantLimitsState.Initial, logger)
    {
        _set = set ?? throw new ArgumentNullException(nameof(set));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
    }

    public void Edit(PlatformPlanLimitKind kind)
    {
        SetState(new TenantLimitsState(kind, null, false, null, null));
    }

    public void AskRemove(Guid overrideId)
    {
        SetState(new TenantLimitsState(null, overrideId, false, null, null));
    }

    public void Cancel()
    {
        SetState(TenantLimitsState.Initial);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> ApplyAsync(Guid platformTenantId, int value, DateTime? endsOn, string? reason)
    {
        Error? error;

        if (State.Removing is Guid overrideId)
        {
            error = await RunUseCaseAsync(async (context, cancellationToken) => (await _remove.ExecuteAsync(
                context, new RemovePlatformTenantLimitOverrideRequest { PlatformTenantId = platformTenantId, OverrideId = overrideId }, cancellationToken)).Error);
        }
        else if (State.Editing is PlatformPlanLimitKind kind)
        {
            DateTimeOffset? ends = endsOn is DateTime date ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)) : null;
            error = await RunUseCaseAsync(async (context, cancellationToken) => (await _set.ExecuteAsync(
                context,
                new SetPlatformTenantLimitOverrideRequest { PlatformTenantId = platformTenantId, Kind = kind, Value = value, EndsUtc = ends, Reason = reason ?? string.Empty },
                cancellationToken)).Error);
        }
        else
        {
            return false;
        }

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(error is null
            ? new TenantLimitsState(null, null, false, State.Removing is null ? "ui.subscriptions.override_saved" : "ui.subscriptions.override_removed", null)
            : State with { StepUpPending = false, Error = error });

        return error is null;
    }
}
