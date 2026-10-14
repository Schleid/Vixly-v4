using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Application.UseCases.Users.EndPlatformUserSessions;
using Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;
using Vixly.Platform.Application.UseCases.Users.ResendPlatformUserInvitation;
using Vixly.Platform.Application.UseCases.Users.ResetPlatformUserSecondFactor;
using Vixly.Platform.Application.UseCases.Users.SendPlatformUserPasswordReset;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserSecurityActionsPresenter.cs
//
// PURPOSE:
// Behaviour of the sign-in and security panel: reset second
// factor, end sessions, resend invitation, send password reset.
//
// RESPONSIBILITIES:
// - Run the security use case the administrator chose
//
// IMPORTANT NOTES:
// - Reset second factor and end sessions need a step-up;
//   the two email actions do not (design section 4)
// =========================================================
public sealed class UserSecurityActionsPresenter : UserActionsPresenter
{
    private static readonly PlatformUserAdminAction[] Actions =
    {
        PlatformUserAdminAction.ResendInvitation,
        PlatformUserAdminAction.SendPasswordReset,
        PlatformUserAdminAction.EndSessions,
        PlatformUserAdminAction.ResetSecondFactor
    };

    private readonly ResetPlatformUserSecondFactorUseCase _resetSecondFactor;

    private readonly EndPlatformUserSessionsUseCase _endSessions;

    private readonly ResendPlatformUserInvitationUseCase _resendInvitation;

    private readonly SendPlatformUserPasswordResetUseCase _sendPasswordReset;

    public UserSecurityActionsPresenter(
        GetPlatformUserActionsUseCase actions,
        ResetPlatformUserSecondFactorUseCase resetSecondFactor,
        EndPlatformUserSessionsUseCase endSessions,
        ResendPlatformUserInvitationUseCase resendInvitation,
        SendPlatformUserPasswordResetUseCase sendPasswordReset,
        PlatformUseCaseScope scope,
        ILogger<UserSecurityActionsPresenter> logger)
        : base(actions, scope, logger)
    {
        _resetSecondFactor = resetSecondFactor ?? throw new ArgumentNullException(nameof(resetSecondFactor));
        _endSessions = endSessions ?? throw new ArgumentNullException(nameof(endSessions));
        _resendInvitation = resendInvitation ?? throw new ArgumentNullException(nameof(resendInvitation));
        _sendPasswordReset = sendPasswordReset ?? throw new ArgumentNullException(nameof(sendPasswordReset));
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
            PlatformUserAdminAction.ResetSecondFactor => (await _resetSecondFactor.ExecuteAsync(
                context, new ResetPlatformUserSecondFactorRequest { PlatformUserId = platformUserId, Reason = reason }, cancellationToken)).Error,
            PlatformUserAdminAction.EndSessions => (await _endSessions.ExecuteAsync(
                context, new EndPlatformUserSessionsRequest { PlatformUserId = platformUserId }, cancellationToken)).Error,
            PlatformUserAdminAction.ResendInvitation => (await _resendInvitation.ExecuteAsync(
                context, new ResendPlatformUserInvitationRequest { PlatformUserId = platformUserId }, cancellationToken)).Error,
            PlatformUserAdminAction.SendPasswordReset => (await _sendPasswordReset.ExecuteAsync(
                context, new SendPlatformUserPasswordResetRequest { PlatformUserId = platformUserId }, cancellationToken)).Error,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
