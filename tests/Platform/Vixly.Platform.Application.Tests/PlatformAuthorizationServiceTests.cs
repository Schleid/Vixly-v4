using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformAuthorizationServiceTests.cs
//
// PURPOSE:
// Proves permissions are decided from persistence at the time
// of the check.
//
// RESPONSIBILITIES:
// - Unknown user, inactive account and unknown code deny
// - SuperUser authority allows everything
// - Roles allow their codes only while active and in force
//
// IMPORTANT NOTES:
// - The authority in the context is ignored on purpose; a
//   demotion counts at once
// =========================================================
public sealed class PlatformAuthorizationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FixedTimeProvider _clock = new(Now);

    [Fact]
    public async Task UnknownUser_IsDenied()
    {
        Assert.False(await Service().IsAuthorizedAsync(Context(Guid.CreateVersion7(Now)), PlatformPermissions.UsersView));
    }

    [Fact]
    public async Task InvitedOrLockedAccount_IsDenied_EvenForSuperUser()
    {
        PlatformUser superUser = AddUser(PlatformUserAuthorityLevel.SuperUser, activate: false);

        Assert.False(await Service().IsAuthorizedAsync(Context(superUser.Id), PlatformPermissions.UsersView));
    }

    [Fact]
    public async Task SuperUser_IsAllowedEverything_ButNotUnknownCodes()
    {
        PlatformUser superUser = AddUser(PlatformUserAuthorityLevel.SuperUser);

        Assert.True(await Service().IsAuthorizedAsync(Context(superUser.Id), PlatformPermissions.AuditView));
        Assert.False(await Service().IsAuthorizedAsync(Context(superUser.Id), "platform.anything.made_up"));
    }

    [Fact]
    public async Task ContextAuthority_IsNotTrusted()
    {
        PlatformUser user = AddUser(PlatformUserAuthorityLevel.Standard);
        PlatformAuthorizationContext forged = new(user.Id, PlatformUserAuthorityLevel.SuperUser);

        Assert.False(await Service().IsAuthorizedAsync(forged, PlatformPermissions.UsersView));
    }

    [Fact]
    public async Task Role_GrantsItsCodes_WhileActiveAndInForce()
    {
        PlatformUser user = AddUser(PlatformUserAuthorityLevel.Standard);
        PlatformRole role = PlatformRole.CreateCustom("Support", Now);
        role.ReplacePermissions(new[] { PlatformPermissions.UsersView }, Now);
        _store.Roles.Add(role);
        user.AssignRole(role.Id, Guid.CreateVersion7(Now), Now.AddHours(1), Now);

        Assert.True(await Service().IsAuthorizedAsync(Context(user.Id), PlatformPermissions.UsersView));
        Assert.False(await Service().IsAuthorizedAsync(Context(user.Id), PlatformPermissions.UsersLock));

        _clock.Advance(TimeSpan.FromHours(1));
        Assert.False(await Service().IsAuthorizedAsync(Context(user.Id), PlatformPermissions.UsersView));
    }

    [Fact]
    public async Task InactiveRole_GrantsNothing()
    {
        PlatformUser user = AddUser(PlatformUserAuthorityLevel.Standard);
        PlatformRole role = PlatformRole.CreateCustom("Support", Now);
        role.ReplacePermissions(new[] { PlatformPermissions.UsersView }, Now);
        role.Deactivate(Now);
        _store.Roles.Add(role);
        user.AssignRole(role.Id, Guid.CreateVersion7(Now), null, Now);

        Assert.False(await Service().IsAuthorizedForAnyAsync(Context(user.Id), new[] { PlatformPermissions.UsersView }));
    }

    [Fact]
    public void SystemRoles_UseOnlyKnownCodes()
    {
        foreach (IReadOnlyCollection<string> codes in PlatformSystemRoles.Definitions.Values)
        {
            Assert.All(codes, code => Assert.True(PlatformPermissions.IsKnown(code), code));
        }
    }

    private PlatformAuthorizationService Service()
    {
        return new PlatformAuthorizationService(_store, _clock);
    }

    private static PlatformAuthorizationContext Context(Guid userId)
    {
        return new PlatformAuthorizationContext(userId, PlatformUserAuthorityLevel.Standard);
    }

    private PlatformUser AddUser(PlatformUserAuthorityLevel level, bool activate = true)
    {
        PlatformUser user = PlatformUser.Invite(
            "Test",
            "User",
            EmailAddress.Create($"u{_store.Users.Count}@example.com"),
            LanguageCode.En,
            level,
            null,
            null,
            Now);

        if (activate)
        {
            user.AcceptInvitation("$2a$12$hash", Now);
        }

        _store.Users.Add(user);

        return user;
    }
}
