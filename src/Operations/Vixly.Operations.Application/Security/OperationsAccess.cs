using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
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
// - A partner agent (Slice 7 step 4, R-8) holds no role: the partner's access at its warehouse, while the plan includes it
// - An outside account (S-5) works only during its hours and until its end date
// =========================================================
public sealed class OperationsAccess
{
    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsSessionRepository _sessions;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsPlanFeatures _features;

    public OperationsAccess(IOperationsUserRepository users, IOperationsRoleRepository roles, IOperationsSessionRepository sessions, TimeProvider timeProvider,
        IOperationsLocationRepository locations, IOperationsPlanFeatures features, IOperationsSettingsRepository settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OperationsAccessView?> LoadAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Slice 11 (review): a scheduled job's context is never a signed-in person here.
        return context.IsScheduledWork ? null : await ViewAsync(context, cancellationToken);
    }

    // Only for the use cases a schedule runs on a person's behalf (the scheduled reports): their rights now, without a session.
    public async Task<OperationsAccessView?> LoadScheduledAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.IsScheduledWork ? await ViewAsync(context, cancellationToken) : await LoadAsync(context, cancellationToken);
    }

    private async Task<OperationsAccessView?> ViewAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null || !user.Account.IsActive)
        {
            return null;
        }

        // Owner, 2026-10-29 (S-5): an outside account outside its hours, or after its end date, does nothing; its open session stops working at once.
        if (!await MayWorkNowAsync(user, cancellationToken))
        {
            return null;
        }

        IReadOnlySet<string> withheld = context.TenantId is { } tenantId
            ? Vixly.Operations.Application.Plans.OperationsPlanModules.WithheldPermissions(await _features.GetAsync(tenantId, cancellationToken))
            : new HashSet<string>(StringComparer.Ordinal);

        if (user.PartnerWarehouseId is { } partnerWarehouseId)
        {
            return new OperationsAccessView(user, Array.Empty<(OperationsRoleGrant, OperationsRole)>(), await PartnerScopeAsync(context, partnerWarehouseId, cancellationToken)) { PlanWithheld = withheld };
        }

        IReadOnlyList<OperationsRoleGrant> grants = (await _roles.ListGrantsAsync(user.Id, cancellationToken)).Where(grant => grant.IsActive).ToList();
        Dictionary<Guid, OperationsRole> roles = grants.Count == 0
            ? new Dictionary<Guid, OperationsRole>()
            : (await _roles.ListAsync(cancellationToken)).ToDictionary(role => role.Id);

        return new OperationsAccessView(user, grants
            .Where(grant => roles.ContainsKey(grant.RoleId))
            .Select(grant => (grant, roles[grant.RoleId]))
            .ToList()) { PlanWithheld = withheld };
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
        // Require MFA = No (owner, 2026-10-22): they confirm with the password instead.
        bool canStepUp = view.User.HasSecondFactor || (await _settings.FindAsync(cancellationToken))?.RequireMfa == false;
        return stepUp && (!canStepUp || !await IsFreshAsync(context, cancellationToken)) ? (view, OperationsAccessErrors.StepUpRequired) : (view, null);
    }

    // Open only while the plan includes partner agents (fails closed: no answer from Platform, no access) and the warehouse is run by a partner.
    public async Task<OperationsPartnerScope> PartnerScopeAsync(OperationsAuthorizationContext context, Guid warehouseId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TenantId is not { } tenantId || !(await _features.GetAsync(tenantId, cancellationToken)).PartnerAgents
            || await _locations.GetWarehouseAsync(warehouseId, cancellationToken) is not { OperatedBy: OperationsDestinationOperator.Partner, Status: OperationsLocationStatus.Active } warehouse)
        {
            return new OperationsPartnerScope(warehouseId, false, false);
        }

        return new OperationsPartnerScope(warehouseId, true, warehouse.PartnerHandlesMoney);
    }

    // Owner, 2026-10-29 (S-5): every account may work now, except an outside one outside its hours or after its end date, in the company's time zone.
    public async Task<bool> MayWorkNowAsync(OperationsUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsOutside)
        {
            return true;
        }

        string? zone = (await _settings.FindAsync(cancellationToken))?.TimeZone;
        return user.MayWorkAt(OperationsTimeZones.Local(_timeProvider.GetUtcNow(), zone));
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
