using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users;

// =========================================================
// FILE: PlatformUserAdministration.cs
//
// PURPOSE:
// The checks every administrator action on a staff account
// runs before it changes anything.
//
// RESPONSIBILITIES:
// - Authorize the permission (S-1)
// - Require a recent second factor when the action is
//   sensitive (ADR-0006 step-up)
// - Load the target and refuse a target whose authority level
//   is higher than the actor's
//
// IMPORTANT NOTES:
// - The authority check reads the actor's level fresh, never
//   from the cookie: a demoted administrator loses it at once
// - Unknown target answers NotFound
// =========================================================
public sealed class PlatformUserAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformUserRepository _users;

    private readonly TimeProvider _timeProvider;

    public PlatformUserAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformUserRepository users,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PlatformUserTarget> LoadTargetAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        Guid targetUserId,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        if (requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return PlatformUserTarget.Refused(PlatformCommonErrors.StepUpRequired);
        }

        PlatformUser? user = await _users.GetAsync(targetUserId, cancellationToken);

        if (user is null)
        {
            return PlatformUserTarget.Refused(PlatformCommonErrors.NotFound);
        }

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(
            context.PlatformUserId,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        if (actor is null || actor.AuthorityLevel < user.AuthorityLevel)
        {
            return PlatformUserTarget.Refused(PlatformAuthErrors.TargetAuthorityHigher);
        }

        return PlatformUserTarget.Found(user);
    }
}
