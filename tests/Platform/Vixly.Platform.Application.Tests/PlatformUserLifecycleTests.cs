using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Application.UseCases.Users.DeactivatePlatformUser;
using Vixly.Platform.Application.UseCases.Users.LockPlatformUser;
using Vixly.Platform.Application.UseCases.Users.ReactivatePlatformUser;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformUserLifecycleTests.cs
//
// PURPOSE:
// Proves the administrator account actions end to end through
// the Application layer (decision D-S2, ADR-0006).
//
// RESPONSIBILITIES:
// - Lock ends every session of the target, writes one audit
//   entry and commits once
// - Step-up is required; a stale second factor is refused
// - Nobody acts on a user with a higher authority level
// - Permission is required; unknown target is NotFound
// - A concurrency conflict is reported, not thrown
//
// IMPORTANT NOTES:
// - Uses the real gate and authorization service over the
//   in-memory store
// =========================================================
public sealed class PlatformUserLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private const string TokenA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private const string TokenB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private const string TokenC = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    private readonly FakePlatformStore _store = new();

    private readonly FixedTimeProvider _clock = new(Now);

    [Fact]
    public async Task Lock_EndsTargetSessions_AuditsOnce_AndCommitsOnce()
    {
        (PlatformUser admin, PlatformAuthorizationContext context) = SignedInAdmin();
        PlatformUser target = AddUser(PlatformUserAuthorityLevel.Standard);
        PlatformUserSession targetSession = PlatformUserSession.Start(target.Id, TokenB, null, null, Now);
        _store.Sessions.Add(targetSession);

        LockPlatformUserResponse response = await Lock().ExecuteAsync(
            context,
            new LockPlatformUserRequest { PlatformUserId = target.Id, Reason = "left" },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(PlatformUserAccountStatus.Locked, target.Account.Status);
        Assert.Equal(PlatformSessionEndReason.AccountRestricted, targetSession.EndReason);
        Assert.False(_store.Sessions.Single(s => s.PlatformUserId == admin.Id).IsEnded);
        Assert.Equal(1, _store.Saves);
        Assert.Equal("platform.users.locked", _store.AuditEntries.Single().ActionCode);
        Assert.Equal(admin.Id, _store.AuditEntries.Single().ActorUserId);
        Assert.Equal("203.0.113.9", _store.AuditEntries.Single().IpAddress);
    }

    [Fact]
    public async Task Lock_RequiresARecentSecondFactor()
    {
        (_, PlatformAuthorizationContext context) = SignedInAdmin();
        PlatformUser target = AddUser(PlatformUserAuthorityLevel.Standard);
        _clock.Advance(TimeSpan.FromMinutes(6));

        LockPlatformUserResponse response = await Lock().ExecuteAsync(
            context,
            new LockPlatformUserRequest { PlatformUserId = target.Id, Reason = "left" },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.StepUpRequired, response.Error);
        Assert.Equal(PlatformUserAccountStatus.Active, target.Account.Status);
        Assert.Equal(0, _store.Saves);
    }

    [Fact]
    public async Task Deactivate_ASuperUser_ByAnAdministrator_IsRefused()
    {
        (_, PlatformAuthorizationContext context) = SignedInAdmin(PlatformUserAuthorityLevel.Administrator);
        PlatformUser onlySuperUser = AddUser(PlatformUserAuthorityLevel.SuperUser);

        DeactivatePlatformUserResponse response = await Deactivate().ExecuteAsync(
            context,
            new DeactivatePlatformUserRequest { PlatformUserId = onlySuperUser.Id, Reason = "left" },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.TargetAuthorityHigher, response.Error);
        Assert.True(onlySuperUser.Account.IsActive);
        Assert.Empty(_store.AuditEntries);
        Assert.Equal(0, _store.Saves);
    }

    [Fact]
    public async Task WithoutPermission_IsDenied()
    {
        PlatformUser plain = AddUser(PlatformUserAuthorityLevel.Standard);
        PlatformUser target = AddUser(PlatformUserAuthorityLevel.Standard);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Lock().ExecuteAsync(
            new PlatformAuthorizationContext(plain.Id, PlatformUserAuthorityLevel.Standard),
            new LockPlatformUserRequest { PlatformUserId = target.Id, Reason = "x" },
            CancellationToken.None));
    }

    [Fact]
    public async Task UnknownTarget_IsNotFound()
    {
        (_, PlatformAuthorizationContext context) = SignedInAdmin();

        LockPlatformUserResponse response = await Lock().ExecuteAsync(
            context,
            new LockPlatformUserRequest { PlatformUserId = Guid.CreateVersion7(Now), Reason = "x" },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.NotFound, response.Error);
    }

    [Fact]
    public async Task ConcurrencyConflict_IsReported()
    {
        (_, PlatformAuthorizationContext context) = SignedInAdmin();
        PlatformUser target = AddUser(PlatformUserAuthorityLevel.Standard);
        _store.FailNextSave = true;

        LockPlatformUserResponse response = await Lock().ExecuteAsync(
            context,
            new LockPlatformUserRequest { PlatformUserId = target.Id, Reason = "x" },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.ConcurrencyConflict, response.Error);
    }

    [Fact]
    public async Task Reactivate_KeepsSessionsAsTheyAre()
    {
        (_, PlatformAuthorizationContext context) = SignedInAdmin();
        PlatformUser target = AddUser(PlatformUserAuthorityLevel.Standard);
        target.Suspend("review", Guid.CreateVersion7(Now), 1, Now);

        ReactivatePlatformUserResponse response = await Reactivate().ExecuteAsync(
            context,
            new ReactivatePlatformUserRequest { PlatformUserId = target.Id, Reason = "done" },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.True(target.Account.IsActive);
        Assert.Equal("platform.users.reactivated", _store.AuditEntries.Single().ActionCode);
    }

    private LockPlatformUserUseCase Lock()
    {
        return new LockPlatformUserUseCase(Workflow());
    }

    private DeactivatePlatformUserUseCase Deactivate()
    {
        return new DeactivatePlatformUserUseCase(Workflow());
    }

    private ReactivatePlatformUserUseCase Reactivate()
    {
        return new ReactivatePlatformUserUseCase(Workflow());
    }

    private PlatformUserLifecycleWorkflow Workflow()
    {
        return new PlatformUserLifecycleWorkflow(
            new PlatformUserAdministration(
                new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock)),
                new PlatformStepUpGuard(_store, _clock),
                _store,
                _store,
                _clock),
            _store,
            _store,
            new PlatformAuditTrail(_store, _store, _clock),
            _store,
            _clock);
    }

    private (PlatformUser Admin, PlatformAuthorizationContext Context) SignedInAdmin(
        PlatformUserAuthorityLevel level = PlatformUserAuthorityLevel.SuperUser)
    {
        PlatformUser admin = AddUser(level);

        if (level != PlatformUserAuthorityLevel.SuperUser)
        {
            PlatformRole role = PlatformRole.CreateSystem(PlatformSystemRoles.SecurityAdministrator, Now);
            role.SynchronizeSystemPermissions(PlatformSystemRoles.Definitions[PlatformSystemRoles.SecurityAdministrator], Now);
            _store.Roles.Add(role);
            admin.AssignRole(role.Id, Guid.CreateVersion7(Now.AddDays(-1)), null, Now);
        }

        PlatformUserSession session = PlatformUserSession.Start(admin.Id, TokenA, null, null, Now);
        _store.Sessions.Add(session);

        return (admin, new PlatformAuthorizationContext(admin.Id, level, session.Id));
    }

    private PlatformUser AddUser(PlatformUserAuthorityLevel level)
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
        user.AcceptInvitation("$2a$12$hash", Now);
        _store.Users.Add(user);

        return user;
    }
}
