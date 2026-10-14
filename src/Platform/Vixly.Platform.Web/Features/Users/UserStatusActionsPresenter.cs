using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Application.UseCases.Users.DeactivatePlatformUser;
using Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;
using Vixly.Platform.Application.UseCases.Users.LockPlatformUser;
using Vixly.Platform.Application.UseCases.Users.ReactivatePlatformUser;
using Vixly.Platform.Application.UseCases.Users.SuspendPlatformUser;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserStatusActionsPresenter.cs
//
// PURPOSE:
// Behaviour of the account status panel: lock, suspend,
// deactivate, reactivate (D-S2).
//
// RESPONSIBILITIES:
// - Run the lifecycle use case the administrator chose
//
// IMPORTANT NOTES:
// - Each action needs a reason and a step-up (ADR-0006)
// =========================================================
public sealed class UserStatusActionsPresenter : UserActionsPresenter
{
    private static readonly PlatformUserAdminAction[] Actions =
    {
        PlatformUserAdminAction.Lock,
        PlatformUserAdminAction.Suspend,
        PlatformUserAdminAction.Deactivate,
        PlatformUserAdminAction.Reactivate
    };

    private readonly LockPlatformUserUseCase _lock;

    private readonly SuspendPlatformUserUseCase _suspend;

    private readonly DeactivatePlatformUserUseCase _deactivate;

    private readonly ReactivatePlatformUserUseCase _reactivate;

    public UserStatusActionsPresenter(
        GetPlatformUserActionsUseCase actions,
        LockPlatformUserUseCase lockUser,
        SuspendPlatformUserUseCase suspend,
        DeactivatePlatformUserUseCase deactivate,
        ReactivatePlatformUserUseCase reactivate,
        PlatformUseCaseScope scope,
        ILogger<UserStatusActionsPresenter> logger)
        : base(actions, scope, logger)
    {
        _lock = lockUser ?? throw new ArgumentNullException(nameof(lockUser));
        _suspend = suspend ?? throw new ArgumentNullException(nameof(suspend));
        _deactivate = deactivate ?? throw new ArgumentNullException(nameof(deactivate));
        _reactivate = reactivate ?? throw new ArgumentNullException(nameof(reactivate));
    }

    protected override IReadOnlyCollection<PlatformUserAdminAction> Handles => Actions;

    protected override async Task<Error?> RunActionAsync(
        PlatformUserAdminAction action,
        PlatformAuthorizationContext context,
        Guid platformUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        return action switch
        {
            PlatformUserAdminAction.Lock => (await _lock.ExecuteAsync(
                context, new LockPlatformUserRequest { PlatformUserId = platformUserId, Reason = reason }, cancellationToken)).Error,
            PlatformUserAdminAction.Suspend => (await _suspend.ExecuteAsync(
                context, new SuspendPlatformUserRequest { PlatformUserId = platformUserId, Reason = reason }, cancellationToken)).Error,
            PlatformUserAdminAction.Deactivate => (await _deactivate.ExecuteAsync(
                context, new DeactivatePlatformUserRequest { PlatformUserId = platformUserId, Reason = reason }, cancellationToken)).Error,
            PlatformUserAdminAction.Reactivate => (await _reactivate.ExecuteAsync(
                context, new ReactivatePlatformUserRequest { PlatformUserId = platformUserId, Reason = reason }, cancellationToken)).Error,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
