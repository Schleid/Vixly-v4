using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Roles;
using Vixly.Platform.Application.UseCases.Roles.AssignPlatformRole;
using Vixly.Platform.Application.UseCases.Roles.CreatePlatformRole;
using Vixly.Platform.Application.UseCases.Roles.GetPlatformRole;
using Vixly.Platform.Application.UseCases.Roles.GetPlatformUserRoles;
using Vixly.Platform.Application.UseCases.Roles.ListPermissionHolders;
using Vixly.Platform.Application.UseCases.Roles.ReactivatePlatformRole;
using Vixly.Platform.Application.UseCases.Roles.RevokePlatformRole;
using Vixly.Platform.Application.UseCases.Roles.UpdatePlatformRole;
using Vixly.Platform.Application.UseCases.Roles.UpdateRolePermissions;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformRoleAdministrationTests.cs
//
// PURPOSE:
// Proves the role administration use cases and the system role
// synchronisation.
//
// RESPONSIBILITIES:
// - Synchronisation: creates, corrects, leaves a name conflict
//   alone, and changes nothing on a second run
// - Create and rename: unique names, protected system roles
// - Permissions, assignment, reactivation: step-up, catalogue
//   check and no privilege escalation
// - User roles and permission holders: what the screens show
//
// IMPORTANT NOTES:
// - Uses the real gate, authorization service and step-up
//   guard over the in-memory store
// =========================================================
public sealed class PlatformRoleAdministrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FixedTimeProvider _clock = new(Now);

    private int _tokens;

    [Fact]
    public async Task Synchronizer_CreatesSystemRoles_ThenChangesNothing()
    {
        PlatformSystemRoleSyncResult first = await Synchronizer().SynchronizeAsync(CancellationToken.None);
        PlatformSystemRoleSyncResult second = await Synchronizer().SynchronizeAsync(CancellationToken.None);

        Assert.Equal(new[] { PlatformSystemRoles.SecurityAdministrator, PlatformSystemRoles.Administrator }, first.Created);
        Assert.Empty(second.Created);
        Assert.Empty(second.Corrected);
        Assert.Equal(1, _store.Saves);
        Assert.Equal(2, _store.AuditEntries.Count(entry => entry.ActionCode == "platform.roles.system_created"));
        Assert.True(_store.Roles.Single(role => role.SystemKey == PlatformSystemRoles.Administrator).Grants(PlatformPermissions.UsersView));
    }

    [Fact]
    public async Task Synchronizer_CorrectsDrift_AndLeavesANameConflictAlone()
    {
        PlatformRole drifted = PlatformRole.CreateSystem(PlatformSystemRoles.SecurityAdministrator, Now);
        drifted.SynchronizeSystemPermissions(new[] { PlatformPermissions.AuditView }, Now);
        _store.Roles.Add(drifted);
        _store.Roles.Add(PlatformRole.CreateCustom("Administrator", Now));

        PlatformSystemRoleSyncResult result = await Synchronizer().SynchronizeAsync(CancellationToken.None);

        Assert.Equal(new[] { PlatformSystemRoles.SecurityAdministrator }, result.Corrected);
        Assert.Equal(new[] { PlatformSystemRoles.Administrator }, result.Conflicts);
        Assert.Equal(PlatformSystemRoles.Definitions[PlatformSystemRoles.SecurityAdministrator].Count, drifted.Permissions.Count);
        Assert.DoesNotContain(_store.Roles, role => role.SystemKey == PlatformSystemRoles.Administrator);
    }

    [Fact]
    public async Task CreateRole_RefusesATakenName_IgnoringCase()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        _store.Roles.Add(PlatformRole.CreateCustom("Billing", Now));

        CreatePlatformRoleResponse taken = await Create().ExecuteAsync(context, new CreatePlatformRoleRequest { Name = "  billing " }, CancellationToken.None);
        CreatePlatformRoleResponse created = await Create().ExecuteAsync(context, new CreatePlatformRoleRequest { Name = "Support" }, CancellationToken.None);

        Assert.Equal(PlatformRoleErrors.NameAlreadyUsed, taken.Error);
        Assert.True(created.Succeeded);
        Assert.Equal("Support", _store.Roles.Single(role => role.Id == created.PlatformRoleId).Name);
        Assert.Equal("platform.roles.created", _store.AuditEntries.Single().ActionCode);
    }

    [Fact]
    public async Task UpdateRole_RefusesASystemRole()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        PlatformRole system = PlatformRole.CreateSystem(PlatformSystemRoles.Administrator, Now);
        _store.Roles.Add(system);

        UpdatePlatformRoleResponse response = await Rename().ExecuteAsync(
            context,
            new UpdatePlatformRoleRequest { PlatformRoleId = system.Id, Name = "Admins" },
            CancellationToken.None);

        Assert.Equal(PlatformIdentityErrors.SystemRoleProtected, response.Error);
        Assert.Equal(0, _store.Saves);
    }

    [Fact]
    public async Task UpdatePermissions_RefusesCodesOutsideTheCatalogue()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        PlatformRole role = Custom("Support");

        UpdateRolePermissionsResponse unknown = await Permissions().ExecuteAsync(
            context,
            new UpdateRolePermissionsRequest { PlatformRoleId = role.Id, PermissionCodes = new[] { "platform.nothing" } },
            CancellationToken.None);
        UpdateRolePermissionsResponse authority = await Permissions().ExecuteAsync(
            context,
            new UpdateRolePermissionsRequest { PlatformRoleId = role.Id, PermissionCodes = new[] { PlatformPermissions.UsersChangeAuthority } },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.PermissionUnknown, unknown.Error);
        Assert.Equal(PlatformCommonErrors.PermissionUnknown, authority.Error);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public async Task UpdatePermissions_CannotAddWhatTheActorLacks_ButCanRemove()
    {
        (PlatformUser actor, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        Grant(actor, Custom("Role editors", PlatformPermissions.RolesEditPermissions, PlatformPermissions.UsersView));
        PlatformRole target = Custom("Support", PlatformPermissions.UsersView, PlatformPermissions.AuditView);

        UpdateRolePermissionsResponse add = await Permissions().ExecuteAsync(
            context,
            new UpdateRolePermissionsRequest { PlatformRoleId = target.Id, PermissionCodes = new[] { PlatformPermissions.UsersView, PlatformPermissions.AuditView, PlatformPermissions.UsersLock } },
            CancellationToken.None);
        UpdateRolePermissionsResponse remove = await Permissions().ExecuteAsync(
            context,
            new UpdateRolePermissionsRequest { PlatformRoleId = target.Id, PermissionCodes = new[] { PlatformPermissions.UsersView } },
            CancellationToken.None);

        Assert.Equal(PlatformRoleErrors.ExceedsYourAccess, add.Error);
        Assert.True(remove.Succeeded);
        Assert.Equal(new[] { PlatformPermissions.UsersView }, target.Permissions.Select(permission => permission.PermissionCode));
        Assert.Equal("platform.roles.permissions_changed", _store.AuditEntries.Single().ActionCode);
    }

    [Fact]
    public async Task UpdatePermissions_NeedsARecentSecondFactor()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        PlatformRole role = Custom("Support");
        _clock.Advance(TimeSpan.FromMinutes(6));

        UpdateRolePermissionsResponse response = await Permissions().ExecuteAsync(
            context,
            new UpdateRolePermissionsRequest { PlatformRoleId = role.Id, PermissionCodes = new[] { PlatformPermissions.UsersView } },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.StepUpRequired, response.Error);
    }

    [Fact]
    public async Task AssignRole_GrantsAndAudits_AndRevokeEndsIt()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        PlatformUser target = NewUser(PlatformUserAuthorityLevel.Standard);
        PlatformRole role = Custom("Support", PlatformPermissions.UsersView);
        DateTimeOffset until = Now.AddDays(7);

        AssignPlatformRoleResponse assigned = await Assign().ExecuteAsync(
            context,
            new AssignPlatformRoleRequest { PlatformUserId = target.Id, PlatformRoleId = role.Id, ExpiresUtc = until },
            CancellationToken.None);

        Assert.True(assigned.Succeeded);
        Assert.Equal(until, target.RoleAssignments.Single().ExpiresUtc);
        Assert.Contains(role.Id, target.RolesInForce(Now));

        RevokePlatformRoleResponse revoked = await Revoke().ExecuteAsync(
            context,
            new RevokePlatformRoleRequest { PlatformUserId = target.Id, PlatformRoleId = role.Id },
            CancellationToken.None);

        Assert.True(revoked.Succeeded);
        Assert.Empty(target.RolesInForce(Now));
        Assert.Equal(new[] { "platform.roles.assigned", "platform.roles.revoked" }, _store.AuditEntries.Select(entry => entry.ActionCode));
    }

    [Fact]
    public async Task AssignRole_RefusesEscalation_InactiveRoles_AndYourself()
    {
        (PlatformUser actor, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        Grant(actor, Custom("Assigners", PlatformPermissions.RolesAssign, PlatformPermissions.UsersView));
        PlatformUser target = NewUser(PlatformUserAuthorityLevel.Standard);
        PlatformRole powerful = Custom("Lockers", PlatformPermissions.UsersLock);
        PlatformRole inactive = Custom("Old", PlatformPermissions.UsersView);
        inactive.Deactivate(Now);
        PlatformRole viewers = Custom("Viewers", PlatformPermissions.UsersView);

        Assert.Equal(PlatformRoleErrors.ExceedsYourAccess, (await AssignAsync(context, target, powerful)).Error);
        Assert.Equal(PlatformRoleErrors.RoleInactive, (await AssignAsync(context, target, inactive)).Error);
        Assert.Equal(PlatformIdentityErrors.CannotActOnOwnAccount, (await AssignAsync(context, actor, viewers)).Error);
        Assert.True((await AssignAsync(context, target, viewers)).Succeeded);
    }

    [Fact]
    public async Task AssignRole_RefusesATargetWithHigherAuthority()
    {
        (PlatformUser actor, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        Grant(actor, Custom("Assigners", PlatformPermissions.RolesAssign, PlatformPermissions.UsersView));
        PlatformUser superUser = NewUser(PlatformUserAuthorityLevel.SuperUser);

        AssignPlatformRoleResponse response = await AssignAsync(context, superUser, Custom("Viewers", PlatformPermissions.UsersView));

        Assert.Equal(PlatformAuthErrors.TargetAuthorityHigher, response.Error);
    }

    [Fact]
    public async Task ReactivateRole_NeedsStepUp_AndRefusesEscalation()
    {
        (PlatformUser actor, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        Grant(actor, Custom("Role managers", PlatformPermissions.RolesDeactivate));
        PlatformRole lockers = Custom("Lockers", PlatformPermissions.UsersLock);
        lockers.Deactivate(Now);

        Assert.Equal(PlatformRoleErrors.ExceedsYourAccess, (await ReactivateAsync(context, lockers)).Error);

        _clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await ReactivateAsync(context, lockers)).Error);
        Assert.False(lockers.IsActive);
    }

    [Fact]
    public async Task GetRole_OffersOnlyWhatTheActorMayDo()
    {
        (PlatformUser actor, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        Grant(actor, Custom("Role viewers", PlatformPermissions.RolesView, PlatformPermissions.RolesEdit));
        PlatformRole custom = Custom("Support");
        PlatformRole system = PlatformRole.CreateSystem(PlatformSystemRoles.Administrator, Now);
        _store.Roles.Add(system);

        GetPlatformRoleResponse customRole = await Get().ExecuteAsync(context, new GetPlatformRoleRequest { PlatformRoleId = custom.Id }, CancellationToken.None);
        GetPlatformRoleResponse systemRole = await Get().ExecuteAsync(context, new GetPlatformRoleRequest { PlatformRoleId = system.Id }, CancellationToken.None);

        Assert.Equal(new PlatformRoleActions(true, false, false, false), customRole.Actions);
        Assert.Equal(PlatformRoleActions.None, systemRole.Actions);
    }

    [Fact]
    public async Task GetUserRoles_OffersAssignableRolesWithinYourAccess_NeverForYourself()
    {
        (PlatformUser actor, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        PlatformRole assigners = Custom("Assigners", PlatformPermissions.RolesAssign, PlatformPermissions.UsersView);
        Grant(actor, assigners);
        PlatformRole viewers = Custom("Viewers", PlatformPermissions.UsersView);
        Custom("Lockers", PlatformPermissions.UsersLock);
        PlatformUser target = NewUser(PlatformUserAuthorityLevel.Standard);

        GetPlatformUserRolesResponse other = await UserRoles().ExecuteAsync(context, new GetPlatformUserRolesRequest { PlatformUserId = target.Id }, CancellationToken.None);
        GetPlatformUserRolesResponse self = await UserRoles().ExecuteAsync(context, new GetPlatformUserRolesRequest { PlatformUserId = actor.Id }, CancellationToken.None);

        Assert.True(other.CanManage);
        Assert.Equal(new[] { assigners.Id, viewers.Id }, other.AssignableRoles.Select(role => role.PlatformRoleId));
        Assert.False(self.CanManage);
        Assert.Empty(self.AssignableRoles);
        Assert.Equal(assigners.Id, self.Grants.Single().PlatformRoleId);
    }

    [Fact]
    public async Task ListPermissionHolders_IncludesSuperUsers_AndRefusesUnknownCodes()
    {
        (PlatformUser superUser, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        PlatformUser viewer = NewUser(PlatformUserAuthorityLevel.Standard);
        viewer.AcceptInvitation("H:password", Now);
        Grant(viewer, Custom("Viewers", PlatformPermissions.AuditView));

        ListPermissionHoldersResponse holders = await Holders().ExecuteAsync(context, new ListPermissionHoldersRequest { PermissionCode = PlatformPermissions.AuditView }, CancellationToken.None);
        ListPermissionHoldersResponse unknown = await Holders().ExecuteAsync(context, new ListPermissionHoldersRequest { PermissionCode = "platform.nothing" }, CancellationToken.None);

        Assert.Equal(new[] { superUser.Id, viewer.Id }, holders.Holders.Select(holder => holder.PlatformUserId));
        Assert.Null(holders.Holders[0].RoleName);
        Assert.Equal("Viewers", holders.Holders[1].RoleName);
        Assert.Equal(PlatformCommonErrors.PermissionUnknown, unknown.Error);
    }

    private Task<AssignPlatformRoleResponse> AssignAsync(PlatformAuthorizationContext context, PlatformUser target, PlatformRole role)
    {
        return Assign().ExecuteAsync(
            context,
            new AssignPlatformRoleRequest { PlatformUserId = target.Id, PlatformRoleId = role.Id },
            CancellationToken.None);
    }

    private Task<ReactivatePlatformRoleResponse> ReactivateAsync(PlatformAuthorizationContext context, PlatformRole role)
    {
        return Reactivate().ExecuteAsync(context, new ReactivatePlatformRoleRequest { PlatformRoleId = role.Id }, CancellationToken.None);
    }

    private PlatformSystemRoleSynchronizer Synchronizer()
    {
        return new PlatformSystemRoleSynchronizer(_store, Audit(), _store, _clock);
    }

    private CreatePlatformRoleUseCase Create()
    {
        return new CreatePlatformRoleUseCase(Gate(), _store, Audit(), _store, _clock);
    }

    private UpdatePlatformRoleUseCase Rename()
    {
        return new UpdatePlatformRoleUseCase(Roles(), _store, Audit(), _store, _clock);
    }

    private UpdateRolePermissionsUseCase Permissions()
    {
        return new UpdateRolePermissionsUseCase(Roles(), Audit(), _store, _clock);
    }

    private ReactivatePlatformRoleUseCase Reactivate()
    {
        return new ReactivatePlatformRoleUseCase(Roles(), Audit(), _store, _clock);
    }

    private AssignPlatformRoleUseCase Assign()
    {
        return new AssignPlatformRoleUseCase(Users(), Roles(), _store, Audit(), _store, _clock);
    }

    private RevokePlatformRoleUseCase Revoke()
    {
        return new RevokePlatformRoleUseCase(Users(), Audit(), _store, _clock);
    }

    private GetPlatformRoleUseCase Get()
    {
        return new GetPlatformRoleUseCase(Gate(), _store, Roles(), _clock);
    }

    private GetPlatformUserRolesUseCase UserRoles()
    {
        return new GetPlatformUserRolesUseCase(Gate(), _store, _store, Roles(), _clock);
    }

    private ListPermissionHoldersUseCase Holders()
    {
        return new ListPermissionHoldersUseCase(Gate(), _store, _clock);
    }

    private PlatformRoleAdministration Roles()
    {
        return new PlatformRoleAdministration(Gate(), new PlatformStepUpGuard(_store, _clock), _store, _store, _clock);
    }

    private PlatformUserAdministration Users()
    {
        return new PlatformUserAdministration(Gate(), new PlatformStepUpGuard(_store, _clock), _store, _store, _clock);
    }

    private PlatformAuthorizationGate Gate()
    {
        return new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock));
    }

    private PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(_store, _store, _clock);
    }

    private PlatformRole Custom(string name, params string[] codes)
    {
        PlatformRole role = PlatformRole.CreateCustom(name, Now);
        role.ReplacePermissions(codes, Now);
        _store.Roles.Add(role);

        return role;
    }

    private void Grant(PlatformUser user, PlatformRole role)
    {
        user.AssignRole(role.Id, Guid.NewGuid(), null, Now);
    }

    private (PlatformUser User, PlatformAuthorizationContext Context, PlatformUserSession Session) SignedIn(
        PlatformUserAuthorityLevel level)
    {
        PlatformUser user = NewUser(level);
        user.AcceptInvitation("H:password", Now);
        user.BeginAuthenticatorEnrolment("encrypted-secret", Now);
        user.ConfirmAuthenticatorApp(1, Now);

        PlatformUserSession session = PlatformUserSession.Start(user.Id, NextTokenHash(), null, null, Now);
        _store.Sessions.Add(session);

        return (user, new PlatformAuthorizationContext(user.Id, level, session.Id), session);
    }

    private PlatformUser NewUser(PlatformUserAuthorityLevel level)
    {
        PlatformUser user = PlatformUser.Invite(
            "Test",
            "User" + _store.Users.Count,
            EmailAddress.Create($"r{_store.Users.Count}@example.com"),
            LanguageCode.En,
            level,
            null,
            null,
            Now);
        _store.Users.Add(user);

        return user;
    }

    private string NextTokenHash()
    {
        return (++_tokens).ToString("D64", System.Globalization.CultureInfo.InvariantCulture);
    }
}
