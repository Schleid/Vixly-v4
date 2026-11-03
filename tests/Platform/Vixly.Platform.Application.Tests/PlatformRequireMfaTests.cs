using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactor;
using Vixly.Platform.Application.UseCases.MyAccount.GetStepUpOptions;
using Vixly.Platform.Application.UseCases.SecuritySettings.UpdatePlatformSecuritySettings;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformRequireMfaTests.cs
//
// PURPOSE:
// Control's Require MFA (owner, 2026-10-22): Yes by default;
// with No, people without a second factor sign in and confirm
// with the password, people with one keep using it; only a
// SuperUser changes it, after step-up.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed class PlatformRequireMfaTests
{
    private const string Password = "orbit-lantern-quarry-42";

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FakeCrypto _crypto = new();

    private readonly FixedTimeProvider _clock = new(Now);

    private FakeSecuritySettings _security = new();

    private int _tokens;

    [Fact]
    public async Task ByDefault_APersonWithoutASecondFactor_MustSetOneUp()
    {
        AddUser("ada@example.com", enrolled: false);

        BeginSignInResponse begin = await Begin("ada@example.com");

        Assert.True(begin.EnrolmentRequired);
        Assert.Empty(begin.SessionToken);
        Assert.Empty(_store.Sessions);
    }

    [Fact]
    public async Task RequireMfaNo_APersonWithoutASecondFactor_SignsInWithThePassword()
    {
        _security = FakeSecuritySettings.WithRequireMfa(false, Guid.NewGuid(), Now);
        AddUser("ada@example.com", enrolled: false);

        BeginSignInResponse begin = await Begin("ada@example.com");

        Assert.True(begin.Succeeded);
        Assert.NotEmpty(begin.SessionToken);
        Assert.Empty(begin.ChallengeToken);
        Assert.Single(_store.Sessions);
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.signed_in");
    }

    [Fact]
    public async Task RequireMfaNo_APersonWithASecondFactor_StillUsesIt()
    {
        _security = FakeSecuritySettings.WithRequireMfa(false, Guid.NewGuid(), Now);
        AddUser("ada@example.com", enrolled: true);

        BeginSignInResponse begin = await Begin("ada@example.com");

        Assert.NotEmpty(begin.ChallengeToken);
        Assert.Empty(begin.SessionToken);
        Assert.False(begin.EnrolmentRequired);
        Assert.Empty(_store.Sessions);
    }

    [Fact]
    public async Task RequireMfaNo_WrongPassword_IsStillRefused()
    {
        _security = FakeSecuritySettings.WithRequireMfa(false, Guid.NewGuid(), Now);
        AddUser("ada@example.com", enrolled: false);

        BeginSignInResponse begin = await new BeginSignInUseCase(_store, _crypto, SignInSteps(), Audit(), _store)
            .ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new BeginSignInRequest { Email = "ada@example.com", Password = "wrong-password-123456" }, CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.SignInFailed, begin.Error);
        Assert.Empty(_store.Sessions);
    }

    [Fact]
    public async Task StepUp_WithThePassword_OnlyWhenRequireMfaIsNo_AndThePersonHasNoSecondFactor()
    {
        (PlatformUser _, PlatformAuthorizationContext context) = SignedIn(enrolled: false);

        Assert.False((await new GetStepUpOptionsUseCase(StepUpSteps()).ExecuteAsync(context, new GetStepUpOptionsRequest(), CancellationToken.None)).PasswordOnly);
        Assert.Equal(PlatformAuthErrors.CodeInvalid, (await Confirm(context, Password)).Error);

        _security = FakeSecuritySettings.WithRequireMfa(false, Guid.NewGuid(), Now);

        Assert.True((await new GetStepUpOptionsUseCase(StepUpSteps()).ExecuteAsync(context, new GetStepUpOptionsRequest(), CancellationToken.None)).PasswordOnly);
        Assert.Equal(PlatformAuthErrors.PasswordIncorrect, (await Confirm(context, "not-the-password-0000")).Error);
        Assert.True((await Confirm(context, Password)).Succeeded);
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.step_up_confirmed");
    }

    [Fact]
    public async Task StepUp_APersonWithASecondFactor_CannotUseThePassword()
    {
        _security = FakeSecuritySettings.WithRequireMfa(false, Guid.NewGuid(), Now);
        (PlatformUser _, PlatformAuthorizationContext context) = SignedIn(enrolled: true);

        Assert.Equal(PlatformAuthErrors.CodeInvalid, (await Confirm(context, Password)).Error);
    }

    [Fact]
    public async Task OnlyASuperUser_ChangesIt_AfterStepUp_AndItIsRecorded()
    {
        (PlatformUser _, PlatformAuthorizationContext standard) = SignedIn(enrolled: true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Update(standard, false));

        (PlatformUser admin, PlatformAuthorizationContext superUser) = SignedIn(enrolled: true, PlatformUserAuthorityLevel.SuperUser);
        PlatformUserSession adminSession = _store.Sessions.Single(session => session.PlatformUserId == admin.Id);

        for (int minute = 0; minute < 6; minute++)
        {
            _clock.Advance(TimeSpan.FromMinutes(2));
            adminSession.Touch(_clock.GetUtcNow());
        }

        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await Update(superUser, false)).Error);
        Assert.Null(_security.Settings);

        adminSession.ConfirmSecondFactor(_clock.GetUtcNow());
        Assert.True((await Update(superUser, false)).Succeeded);
        Assert.False(_security.Settings!.RequireMfa);
        Assert.Equal(admin.Id, _security.Settings.UpdatedByUserId);
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.security.require_mfa_changed");
    }

    // Owner, 2026-10-27: the security administrator sets Control's inactivity sign-out; sessions follow it.
    [Fact]
    public async Task IdleTimes_AreChangedWithTheSessionsRight_AfterStepUp_AndSessionsFollowThem()
    {
        FakeIdleTimes idle = new(_security);
        (PlatformUser _, PlatformAuthorizationContext standard) = SignedIn(enrolled: true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ChangeIdle(standard, idle, 20, 3));

        (PlatformUser admin, PlatformAuthorizationContext superUser) = SignedIn(enrolled: true, PlatformUserAuthorityLevel.SuperUser);
        PlatformUserSession adminSession = _store.Sessions.Single(session => session.PlatformUserId == admin.Id);
        _clock.Advance(TimeSpan.FromMinutes(4));
        adminSession.Touch(_clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromMinutes(4));
        adminSession.Touch(_clock.GetUtcNow());
        _clock.Advance(TimeSpan.FromMinutes(4));
        adminSession.Touch(_clock.GetUtcNow());

        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await ChangeIdle(superUser, idle, 20, 3)).Error);
        adminSession.ConfirmSecondFactor(_clock.GetUtcNow());
        Assert.Equal("platform.identity.idle_invalid", (await ChangeIdle(superUser, idle, 45, 3)).Error?.Code);
        Assert.True((await ChangeIdle(superUser, idle, 20, 3)).Succeeded);
        Assert.Equal((20, 3), (_security.Settings!.IdleMinutes, _security.Settings.IdleWarnMinutes));
        Assert.Equal(1, idle.Forgotten);
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.security.idle_changed");

        // Twelve quiet minutes: past the old 5, inside the new 20.
        _clock.Advance(TimeSpan.FromMinutes(12));
        Assert.True(adminSession.IsValidAt(_clock.GetUtcNow(), (await idle.GetAsync(CancellationToken.None)).Idle));
        Assert.False(adminSession.IsValidAt(_clock.GetUtcNow()));
    }

    private Task<Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes.ChangePlatformIdleTimesResponse> ChangeIdle(
        PlatformAuthorizationContext context, FakeIdleTimes idle, int minutes, int warn) =>
        new Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes.ChangePlatformIdleTimesUseCase(
                new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock)),
                new PlatformStepUpGuard(_store, _clock, idle),
                _security,
                idle,
                Audit(),
                _store,
                _clock)
            .ExecuteAsync(context, new Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes.ChangePlatformIdleTimesRequest { IdleMinutes = minutes, IdleWarnMinutes = warn },
                CancellationToken.None);

    private Task<BeginSignInResponse> Begin(string email)
    {
        return new BeginSignInUseCase(_store, _crypto, SignInSteps(), Audit(), _store)
            .ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new BeginSignInRequest { Email = email, Password = Password }, CancellationToken.None);
    }

    private Task<ConfirmSecondFactorResponse> Confirm(PlatformAuthorizationContext context, string password)
    {
        return new ConfirmSecondFactorUseCase(StepUpSteps(), _crypto, _crypto, _crypto)
            .ExecuteAsync(context, new ConfirmSecondFactorRequest { Code = password, UsePassword = true }, CancellationToken.None);
    }

    private Task<UpdatePlatformSecuritySettingsResponse> Update(PlatformAuthorizationContext context, bool requireMfa)
    {
        return new UpdatePlatformSecuritySettingsUseCase(
                new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock)),
                new PlatformStepUpGuard(_store, _clock),
                _security,
                Audit(),
                _store,
                _clock)
            .ExecuteAsync(context, new UpdatePlatformSecuritySettingsRequest { RequireMfa = requireMfa }, CancellationToken.None);
    }

    private PlatformSignInSteps SignInSteps() => new(_store, _store, _store, _crypto, _store, Audit(), _store, _clock, _security);

    private PlatformStepUpSteps StepUpSteps() => new(_store, _store, Audit(), _store, _clock, _security);

    private PlatformAuditTrail Audit() => new(_store, _store, _clock);

    private PlatformUser AddUser(string email, bool enrolled, PlatformUserAuthorityLevel level = PlatformUserAuthorityLevel.Standard)
    {
        PlatformUser user = PlatformUser.Invite("Ada", "Lovelace", EmailAddress.Create(email), LanguageCode.En, level, null, null, Now);
        user.AcceptInvitation("H:" + Password, Now);

        if (enrolled)
        {
            user.BeginAuthenticatorEnrolment("encrypted-secret", Now);
            user.ConfirmAuthenticatorApp(1, Now);
        }

        _store.Users.Add(user);

        return user;
    }

    private (PlatformUser User, PlatformAuthorizationContext Context) SignedIn(bool enrolled, PlatformUserAuthorityLevel level = PlatformUserAuthorityLevel.Standard)
    {
        PlatformUser user = AddUser($"u{_store.Users.Count}@example.com", enrolled, level);
        PlatformUserSession session = PlatformUserSession.Start(user.Id, (++_tokens).ToString("D64", System.Globalization.CultureInfo.InvariantCulture), null, null, Now);
        _store.Sessions.Add(session);

        return (user, new PlatformAuthorizationContext(user.Id, level, session.Id));
    }
}
