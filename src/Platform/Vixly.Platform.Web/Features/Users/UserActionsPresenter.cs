using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserActionsPresenter.cs
//
// PURPOSE:
// The shared behaviour of the administrator action panels.
//
// RESPONSIBILITIES:
// - Offer the actions this panel handles that the Application
//   says are available
// - Run the chosen action; when it needs a step-up, hold it, ask,
//   and run it again after the confirmation
// - Report what was done, or the error
//
// IMPORTANT NOTES:
// - Which actions apply is decided in Application
//   (PlatformUserAdminActionRules), never here
// - Subclasses name their actions and run their use cases; this
//   keeps each presenter within 7 dependencies (UI-7)
// =========================================================
public abstract class UserActionsPresenter : PlatformPresenter<UserActionsState>
{
    private readonly GetPlatformUserActionsUseCase _actions;

    protected UserActionsPresenter(
        GetPlatformUserActionsUseCase actions,
        PlatformUseCaseScope scope,
        ILogger logger)
        : base(scope, UserActionsState.Initial, logger)
    {
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
    }

    protected abstract IReadOnlyCollection<PlatformUserAdminAction> Handles { get; }

    public static bool RequiresReason(PlatformUserAdminAction action)
    {
        return action is PlatformUserAdminAction.Lock
            or PlatformUserAdminAction.Suspend
            or PlatformUserAdminAction.Deactivate
            or PlatformUserAdminAction.Reactivate
            or PlatformUserAdminAction.ResetSecondFactor;
    }

    public async Task LoadAsync(Guid platformUserId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            GetPlatformUserActionsResponse response = await _actions.ExecuteAsync(
                context,
                new GetPlatformUserActionsRequest { PlatformUserId = platformUserId },
                cancellationToken);

            if (response.Succeeded)
            {
                SetState(State with { Available = response.Actions.Where(Handles.Contains).ToList() });
            }

            return response.Error;
        });

        if (error is not null)
        {
            SetState(State with { Available = Array.Empty<PlatformUserAdminAction>(), Error = error });
        }
    }

    public void Select(PlatformUserAdminAction action)
    {
        SetState(State with { Selected = action, Done = null, Error = null });
    }

    public void Cancel()
    {
        SetState(State with { Selected = null, Error = null, StepUpPending = false, PendingReason = string.Empty });
    }

    public async Task<bool> SubmitAsync(Guid platformUserId, string reason)
    {
        if (State.Selected is not PlatformUserAdminAction action)
        {
            return false;
        }

        string typedReason = reason ?? string.Empty;
        Error? error = await RunUseCaseAsync((context, cancellationToken) =>
            RunActionAsync(action, context, platformUserId, typedReason, cancellationToken));

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, PendingReason = typedReason, Error = null });
            return false;
        }

        if (error is not null)
        {
            SetState(State with { Error = error });
            return false;
        }

        SetState(State with { Selected = null, Done = action, Error = null, StepUpPending = false, PendingReason = string.Empty });
        await LoadAsync(platformUserId);

        return true;
    }

    public Task<bool> ResumeAfterStepUpAsync(Guid platformUserId)
    {
        string reason = State.PendingReason;
        SetState(State with { StepUpPending = false });

        return SubmitAsync(platformUserId, reason);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    protected abstract Task<Error?> RunActionAsync(
        PlatformUserAdminAction action,
        PlatformAuthorizationContext context,
        Guid platformUserId,
        string reason,
        CancellationToken cancellationToken);
}
