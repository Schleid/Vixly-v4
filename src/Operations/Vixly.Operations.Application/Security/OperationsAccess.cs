using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsAccess.cs
//
// PURPOSE:
// Loads what the signed-in staff member may do and checks permissions and step-up (Slice 1D).
//
// RESPONSIBILITIES:
// - Load the user with their active grants and roles
// - Refuse without the permission, or without a fresh second factor when asked
// - Create the Administrator role the first time it is needed
//
// IMPORTANT NOTES:
// - Only active accounts act; inactive roles grant nothing
// - Step-up: the session confirmed a second factor within 5 minutes
// =========================================================
public sealed class OperationsAccess
{
    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsSessionRepository _sessions;

    private readonly TimeProvider _timeProvider;

    public OperationsAccess(IOperationsUserRepository users, IOperationsRoleRepository roles, IOperationsSessionRepository sessions, TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OperationsAccessView?> LoadAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null || !user.Account.IsActive)
        {
            return null;
        }

        IReadOnlyList<OperationsRoleGrant> grants = (await _roles.ListGrantsAsync(user.Id, cancellationToken)).Where(grant => grant.IsActive).ToList();
        Dictionary<Guid, OperationsRole> roles = grants.Count == 0
            ? new Dictionary<Guid, OperationsRole>()
            : (await _roles.ListAsync(cancellationToken)).ToDictionary(role => role.Id);

        return new OperationsAccessView(user, grants
            .Where(grant => roles.ContainsKey(grant.RoleId))
            .Select(grant => (grant, roles[grant.RoleId]))
            .ToList());
    }

    public async Task<(OperationsAccessView? View, Error? Refused)> RequireAsync(OperationsAuthorizationContext context, string permission, bool stepUp, CancellationToken cancellationToken)
    {
        OperationsAccessView? view = await LoadAsync(context, cancellationToken);

        if (view is null)
        {
            return (null, OperationsAccessErrors.SignInRequired);
        }

        // Staff, roles, settings, company, history and devices are company matters: only a grant for the whole company counts.
        // Branch and warehouse grants count for locations only (CoversBranch, CoversWarehouse).
        if (!view.HasTenantWide(permission))
        {
            return (view, OperationsAccessErrors.PermissionRequired);
        }

        // A user without a second factor can never step up: they enrol one first (design section 3).
        return stepUp && (!view.User.HasSecondFactor || !await IsFreshAsync(context, cancellationToken)) ? (view, OperationsAccessErrors.StepUpRequired) : (view, null);
    }

    public async Task<bool> IsFreshAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        OperationsUserSession? session = context.SessionId is { } sessionId ? await _sessions.GetAsync(sessionId, cancellationToken) : null;
        return session is not null && session.SecondFactorUtc >= _timeProvider.GetUtcNow() - OperationsAccessLimits.StepUpWindow;
    }

    public async Task<OperationsRole> AdministratorRoleAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await _roles.FindAdministratorAsync(cancellationToken) is { } existing)
        {
            return existing;
        }

        OperationsRole created = OperationsRole.CreateAdministrator(tenantId, _timeProvider.GetUtcNow());
        _roles.Add(created);
        return created;
    }
}
