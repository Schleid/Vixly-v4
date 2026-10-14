using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.DeactivatePlatformUser;

// =========================================================
// FILE: DeactivatePlatformUserUseCase.cs
//
// PURPOSE:
// An administrator deactivates the account of a staff member who has left.
//
// RESPONSIBILITIES:
// - Require UsersDeactivate and a recent second factor
// - Apply the domain rule and record it in the audit log
//
// IMPORTANT NOTES:
// - Ends every session of the user at once (ADR-0006)
// - History, grants and audit stay; nothing is deleted
// =========================================================
public sealed class DeactivatePlatformUserUseCase
{
    private readonly PlatformUserLifecycleWorkflow _workflow;

    public DeactivatePlatformUserUseCase(PlatformUserLifecycleWorkflow workflow)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
    }

    public async Task<DeactivatePlatformUserResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DeactivatePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Error? error = await _workflow.RunAsync(
            context,
            new PlatformUserLifecycleAction(
                request.PlatformUserId,
                PlatformPermissions.UsersDeactivate,
                "platform.users.deactivated",
                (user, actor, otherSuperUsers, utcNow) => user.Deactivate(request.Reason, actor, otherSuperUsers, utcNow),
                PlatformSessionEndReason.AccountRestricted),
            cancellationToken);

        return error is null
            ? DeactivatePlatformUserResponse.Success()
            : DeactivatePlatformUserResponse.Failure(error);
    }
}
