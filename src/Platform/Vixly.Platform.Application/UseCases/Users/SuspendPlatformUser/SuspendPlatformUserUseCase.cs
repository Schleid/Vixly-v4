using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.SuspendPlatformUser;

// =========================================================
// FILE: SuspendPlatformUserUseCase.cs
//
// PURPOSE:
// An administrator suspends a staff account, for example during a review.
//
// RESPONSIBILITIES:
// - Require UsersSuspend and a recent second factor
// - Apply the domain rule and record it in the audit log
//
// IMPORTANT NOTES:
// - Ends every session of the user at once (ADR-0006)
// - Decision D-S2 (identity sweep)
// =========================================================
public sealed class SuspendPlatformUserUseCase
{
    private readonly PlatformUserLifecycleWorkflow _workflow;

    public SuspendPlatformUserUseCase(PlatformUserLifecycleWorkflow workflow)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
    }

    public async Task<SuspendPlatformUserResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SuspendPlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? error = await _workflow.RunAsync(
            context,
            new PlatformUserLifecycleAction(
                request.PlatformUserId,
                PlatformPermissions.UsersSuspend,
                "platform.users.suspended",
                (user, actor, otherSuperUsers, utcNow) => user.Suspend(request.Reason, actor, otherSuperUsers, utcNow),
                PlatformSessionEndReason.AccountRestricted),
            cancellationToken);

        return error is null
            ? SuspendPlatformUserResponse.Success()
            : SuspendPlatformUserResponse.Failure(error);
    }
}
