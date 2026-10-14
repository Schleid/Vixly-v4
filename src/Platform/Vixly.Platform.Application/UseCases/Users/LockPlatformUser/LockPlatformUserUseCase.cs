using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.LockPlatformUser;

// =========================================================
// FILE: LockPlatformUserUseCase.cs
//
// PURPOSE:
// An administrator locks a staff account so it cannot sign in.
//
// RESPONSIBILITIES:
// - Require UsersLock and a recent second factor
// - Apply the domain rule and record it in the audit log
//
// IMPORTANT NOTES:
// - Ends every session of the user at once (ADR-0006)
// - Decision D-S2 (identity sweep)
// =========================================================
public sealed class LockPlatformUserUseCase
{
    private readonly PlatformUserLifecycleWorkflow _workflow;

    public LockPlatformUserUseCase(PlatformUserLifecycleWorkflow workflow)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
    }

    public async Task<LockPlatformUserResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        LockPlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? error = await _workflow.RunAsync(
            context,
            new PlatformUserLifecycleAction(
                request.PlatformUserId,
                PlatformPermissions.UsersLock,
                "platform.users.locked",
                (user, actor, otherSuperUsers, utcNow) => user.Lock(request.Reason, actor, otherSuperUsers, utcNow),
                PlatformSessionEndReason.AccountRestricted),
            cancellationToken);

        return error is null
            ? LockPlatformUserResponse.Success()
            : LockPlatformUserResponse.Failure(error);
    }
}
