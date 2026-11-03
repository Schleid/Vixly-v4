using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.SecuritySettings.GetPlatformSecuritySettings;
using Vixly.Platform.Application.UseCases.SecuritySettings.UpdatePlatformSecuritySettings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: SignInSecurityPresenter.cs
//
// PURPOSE:
// Loads and changes Control's Require MFA (owner, 2026-10-22) and its inactivity sign-out (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Load the rule and whether the reader may change it
// - Save Yes or No; ask for step-up when the use case does
//
// IMPORTANT NOTES:
// - The value waiting for step-up is kept in Pending and saved
//   again once confirmed
// =========================================================
public sealed class SignInSecurityPresenter : PlatformPresenter<SignInSecurityState>
{
    private readonly GetPlatformSecuritySettingsUseCase _get;

    private readonly UpdatePlatformSecuritySettingsUseCase _update;

    private readonly Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes.ChangePlatformIdleTimesUseCase _idle;

    public SignInSecurityPresenter(GetPlatformSecuritySettingsUseCase get, UpdatePlatformSecuritySettingsUseCase update,
        Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes.ChangePlatformIdleTimesUseCase idle, PlatformUseCaseScope scope, ILogger<SignInSecurityPresenter> logger)
        : base(scope, SignInSecurityState.Initial, logger)
    {
        _idle = idle ?? throw new ArgumentNullException(nameof(idle));
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public async Task LoadAsync()
    {
        GetPlatformSecuritySettingsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformSecuritySettingsRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with
            {
                Loaded = true, RequireMfa = response.RequireMfa, UpdatedBy = response.UpdatedBy, UpdatedUtc = response.UpdatedUtc, CanEdit = response.CanEdit,
                IdleMinutes = response.IdleMinutes, IdleWarnMinutes = response.IdleWarnMinutes, CanEditIdle = response.CanEditIdle
            }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { Pending = null, PendingIdle = null });
    }

    public async Task SaveIdleAsync(int idleMinutes, int warnMinutes)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _idle.ExecuteAsync(
            context,
            new Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes.ChangePlatformIdleTimesRequest { IdleMinutes = idleMinutes, IdleWarnMinutes = warnMinutes },
            cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { PendingIdle = (idleMinutes, warnMinutes), Error = null, DoneKey = null });
            return;
        }

        SetState(State with { PendingIdle = null, DoneKey = error is null ? "ui.security.saved" : null, Error = error });

        if (error is null)
        {
            await LoadAsync();
        }
    }

    public async Task SaveAsync(bool requireMfa)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformSecuritySettingsRequest { RequireMfa = requireMfa },
            cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { Pending = requireMfa, Error = null, DoneKey = null });
            return;
        }

        SetState(State with { Pending = null, DoneKey = error is null ? "ui.security.saved" : null, Error = error });

        if (error is null)
        {
            await LoadAsync();
        }
    }

    public Task ConfirmPendingAsync() =>
        State.Pending is bool pending ? SaveAsync(pending)
        : State.PendingIdle is { } idle ? SaveIdleAsync(idle.Idle, idle.Warn)
        : Task.CompletedTask;
}
