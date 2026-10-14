using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.ReactivatePlatformUser;

// =========================================================
// FILE: ReactivatePlatformUserUseCase.cs
//
// PURPOSE:
// An administrator restores a locked, suspended or deactivated staff account.
//
// RESPONSIBILITIES:
// - Require UsersReactivate and a recent second factor
// - Apply the domain rule and record it in the audit log
//
// IMPORTANT NOTES:
// - An account that was never activated returns to Invited
// - Sessions are not touched: the user signs in again
// =========================================================
public sealed class ReactivatePlatformUserUseCase
{
    private readonly PlatformUserLifecycleWorkflow _workflow;

    public ReactivatePlatformUserUseCase(PlatformUserLifecycleWorkflow workflow)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
    }

    public async Task<ReactivatePlatformUserResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReactivatePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? error = await _workflow.RunAsync(
            context,
            new PlatformUserLifecycleAction(
                request.PlatformUserId,
                PlatformPermissions.UsersReactivate,
                "platform.users.reactivated",
                (user, actor, otherSuperUsers, utcNow) => user.Reactivate(request.Reason, actor, utcNow),
                null),
            cancellationToken);

        return error is null
            ? ReactivatePlatformUserResponse.Success()
            : ReactivatePlatformUserResponse.Failure(error);
    }
}
