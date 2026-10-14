using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles;

// =========================================================
// FILE: PlatformRoleAdministration.cs
//
// PURPOSE:
// The checks every role administration action runs before it
// changes anything.
//
// RESPONSIBILITIES:
// - Authorize the permission (S-1) and require step-up when asked
// - Load the role, or answer NotFound
// - Refuse to hand out permissions the actor does not hold
//   (no privilege escalation)
//
// IMPORTANT NOTES:
// - Reads the actor's access fresh, never from the cookie
// - A SuperUser holds every permission, so the escalation check
//   passes for them
// =========================================================
public sealed class PlatformRoleAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformRoleRepository _roles;

    private readonly TimeProvider _timeProvider;

    public PlatformRoleAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformRoleRepository roles,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // =====================================================
    // METHOD: LOAD ROLE
    // =====================================================
    public async Task<PlatformRoleTarget> LoadRoleAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        Guid platformRoleId,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        if (requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken))
        {
            return PlatformRoleTarget.Refused(PlatformCommonErrors.StepUpRequired);
        }

        PlatformRole? role = await _roles.GetAsync(platformRoleId, cancellationToken);

        return role is null
            ? PlatformRoleTarget.Refused(PlatformCommonErrors.NotFound)
            : PlatformRoleTarget.Found(role);
    }

    // =====================================================
    // METHOD: ACTOR ACCESS
    // =====================================================
    public Task<PlatformPermissionSnapshot?> ReadActorAsync(
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _permissions.ReadAsync(context.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);
    }

    public async Task<Error?> CheckWithinActorAccessAsync(
        PlatformAuthorizationContext context,
        IEnumerable<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(permissionCodes);

        PlatformPermissionSnapshot? actor = await ReadActorAsync(context, cancellationToken);

        if (actor is null || !actor.IsAccountActive)
        {
            return PlatformSecurityErrors.AccessDenied;
        }

        return PlatformAccess.Covers(actor, permissionCodes) ? null : PlatformRoleErrors.ExceedsYourAccess;
    }
}
