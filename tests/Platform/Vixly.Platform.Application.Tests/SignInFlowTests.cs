using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.SignIn.AcceptInvitation;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignInEnrolment;
using Vixly.Platform.Application.UseCases.SignIn.CompleteSignInEnrolment;
using Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithAuthenticatorCode;
using Vixly.Platform.Application.UseCases.SignIn.CompleteSignInWithRecoveryCode;
using Vixly.Platform.Application.UseCases.SignIn.RequestPasswordReset;
using Vixly.Platform.Application.UseCases.SignIn.ResetPassword;
using Vixly.Platform.Application.UseCases.Users.InvitePlatformUser;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: SignInFlowTests.cs
//
// PURPOSE:
// Proves the staff sign-in, invitation and password-reset
// flows of ADR-0006 through the Application layer.
//
// RESPONSIBILITIES:
// - A password alone never opens a session
// - Unknown email and wrong password look the same
// - Lockout after 5 failures
// - Enrolment before access, with 10 recovery codes
// - Authenticator and recovery-code second steps; a pending
//   sign-in dies after 5 wrong codes and is single use
// - Legacy hashes are upgraded at sign-in
// - Invitation: link, password policy, then enrolment
// - Reset: same answer for unknown emails, throttle, sessions
//   end on reset
// - Session validation and the first-SuperUser setup
//
// IMPORTANT NOTES:
// - In-memory store and deterministic crypto fakes
// =========================================================
public sealed class SignInFlowTests
{
    private const string GoodPassword = "orbit-lantern-quarry-42";

    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FakeCrypto _crypto = new();

    private readonly FixedTimeProvider _clock = new(Now);

    private static PlatformAnonymousAuthorizationContext Anonymous => PlatformAnonymousAuthorizationContext.Instance;

    [Fact]
    public async Task UnknownEmail_AndWrongPassword_LookTheSame()
    {
        AddActiveUser("ada@example.com");

        BeginSignInResponse unknown = await Begin("nobody@example.com", GoodPassword);
        BeginSignInResponse wrong = await Begin("ada@example.com", "not-the-password-1");

        Assert.Equal(PlatformAuthErrors.SignInFailed, unknown.Error);
        Assert.Equal(PlatformAuthErrors.SignInFailed, wrong.Error);
        Assert.Equal(2, _crypto.VerifyCalls);
    }

    [Fact]
    public async Task FiveFailures_LockSignIn_EvenWithTheRightPassword()
    {
        AddActiveUser("ada@example.com");

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await Begin("ada@example.com", "not-the-password-1");
        }

        Assert.Equal(PlatformAuthErrors.SignInFailed, (await Begin("ada@example.com", GoodPassword)).Error);

        _clock.Advance(TimeSpan.FromMinutes(15));
        Assert.True((await Begin("ada@example.com", GoodPassword)).Succeeded);
    }

    [Fact]
    public async Task Password_WithoutSecondFactor_LeadsToEnrolment_NotToASession()
    {
        AddActiveUser("ada@example.com");

        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);

        Assert.True(begin.Succeeded);
        Assert.True(begin.EnrolmentRequired);
        Assert.Empty(_store.Sessions);
    }

    [Fact]
    public async Task Enrolment_OpensTheSession_AndGivesTenRecoveryCodes()
    {
        PlatformUser user = AddActiveUser("ada@example.com");
        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);

        BeginSignInEnrolmentResponse setup = await new BeginSignInEnrolmentUseCase(Steps(), _crypto, _store)
            .ExecuteAsync(Anonymous, new BeginSignInEnrolmentRequest { ChallengeToken = begin.ChallengeToken }, CancellationToken.None);
        CompleteSignInEnrolmentResponse done = await new CompleteSignInEnrolmentUseCase(Steps(), _crypto, _crypto)
            .ExecuteAsync(
                Anonymous,
                new CompleteSignInEnrolmentRequest { ChallengeToken = begin.ChallengeToken, Code = FakeCrypto.ValidCode },
                CancellationToken.None);

        Assert.Equal("MANUALKEY", setup.ManualKey);
        Assert.True(done.Succeeded);
        Assert.Equal(10, done.RecoveryCodes.Count);
        Assert.True(user.HasSecondFactor);
        Assert.Single(_store.Sessions);
        Assert.True(_store.Sessions[0].HasRecentSecondFactor(Now));
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.signed_in");
    }

    [Fact]
    public async Task EnrolmentPage_ShownAgain_KeepsTheSameSecret()
    {
        PlatformUser user = AddActiveUser("ada@example.com");
        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);
        BeginSignInEnrolmentUseCase enrol = new(Steps(), _crypto, _store);
        BeginSignInEnrolmentRequest request = new() { ChallengeToken = begin.ChallengeToken };

        await enrol.ExecuteAsync(Anonymous, request, CancellationToken.None);
        PlatformUserAuthenticatorApp first = user.AuthenticatorApp!;
        BeginSignInEnrolmentResponse again = await enrol.ExecuteAsync(Anonymous, request, CancellationToken.None);

        Assert.True(again.Succeeded);
        Assert.Same(first, user.AuthenticatorApp);
    }

    [Fact]
    public async Task PasswordAlone_NeverYieldsASession()
    {
        AddEnrolledUser("ada@example.com");

        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);

        Assert.True(begin.Succeeded);
        Assert.False(string.IsNullOrEmpty(begin.ChallengeToken));
        Assert.Equal(PlatformSignInStage.SecondFactor, _store.Challenges.Single().Stage);
        Assert.Empty(_store.Sessions);
        Assert.DoesNotContain(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.signed_in");
    }

    [Fact]
    public async Task AuthenticatorStep_OpensTheSession_AndTheChallengeIsSingleUse()
    {
        AddEnrolledUser("ada@example.com");
        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);

        CompleteSignInWithAuthenticatorCodeResponse first = await Authenticator(begin.ChallengeToken, FakeCrypto.ValidCode);
        CompleteSignInWithAuthenticatorCodeResponse again = await Authenticator(begin.ChallengeToken, FakeCrypto.ValidCode);

        Assert.False(begin.EnrolmentRequired);
        Assert.True(first.Succeeded);
        Assert.False(string.IsNullOrEmpty(first.SessionToken));
        Assert.Equal(PlatformAuthErrors.SignInExpired, again.Error);
    }

    [Fact]
    public async Task FiveWrongCodes_EndThePendingSignIn()
    {
        AddEnrolledUser("ada@example.com");
        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(PlatformAuthErrors.CodeInvalid, (await Authenticator(begin.ChallengeToken, "000000")).Error);
        }

        Assert.Equal(PlatformAuthErrors.SignInExpired, (await Authenticator(begin.ChallengeToken, FakeCrypto.ValidCode)).Error);
        Assert.Empty(_store.Sessions);
    }

    [Fact]
    public async Task PendingSignIn_ExpiresAfterFiveMinutes()
    {
        AddEnrolledUser("ada@example.com");
        BeginSignInResponse begin = await Begin("ada@example.com", GoodPassword);
        _clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(PlatformAuthErrors.SignInExpired, (await Authenticator(begin.ChallengeToken, FakeCrypto.ValidCode)).Error);
    }

    [Fact]
    public async Task RecoveryCode_SignsIn_Once()
    {
        PlatformUser user = AddEnrolledUser("ada@example.com");
        PlatformSecret[] codes = Enumerable.Range(0, 10).Select(_ => _crypto.NewRecoveryCode()).ToArray();
        user.ReplaceRecoveryCodes(codes.Select(code => code.Hash).ToArray(), Now);

        BeginSignInResponse first = await Begin("ada@example.com", GoodPassword);
        CompleteSignInWithRecoveryCodeResponse used = await Recovery(first.ChallengeToken, codes[0].Value.ToLowerInvariant());
        BeginSignInResponse second = await Begin("ada@example.com", GoodPassword);
        CompleteSignInWithRecoveryCodeResponse reused = await Recovery(second.ChallengeToken, codes[0].Value);

        Assert.True(used.Succeeded);
        Assert.Equal(9, used.RecoveryCodesLeft);
        Assert.Equal(PlatformAuthErrors.CodeInvalid, reused.Error);
    }

    [Fact]
    public async Task LegacyHash_IsUpgradedAtSignIn()
    {
        PlatformUser user = AddActiveUser("ada@example.com", "L:" + GoodPassword);

        Assert.True((await Begin("ada@example.com", GoodPassword)).Succeeded);
        Assert.Equal("H:" + GoodPassword, user.Account.PasswordHash);
    }

    [Fact]
    public async Task Invitation_EnforcesThePasswordPolicy_ThenLeadsToEnrolment()
    {
        PlatformAuthorizationContext admin = SuperUserContext();

        InvitePlatformUserResponse invited = await Invite(admin, "grace@example.com", PlatformUserAuthorityLevel.Standard);
        string token = _store.SentMail.Single(mail => mail.Kind == "invitation").Value;

        AcceptInvitationResponse weak = await Accept(token, "short");
        AcceptInvitationResponse containsEmail = await Accept(token, "grace-is-my-password");
        AcceptInvitationResponse accepted = await Accept(token, GoodPassword);
        AcceptInvitationResponse reused = await Accept(token, GoodPassword);

        Assert.True(invited.Succeeded);
        Assert.Equal(PlatformAuthErrors.PasswordTooShort, weak.Error);
        Assert.Equal(PlatformAuthErrors.PasswordContainsEmail, containsEmail.Error);
        Assert.True(accepted.Succeeded);
        Assert.Equal(PlatformAuthErrors.LinkInvalid, reused.Error);
        Assert.Equal(PlatformSignInStage.Enrolment, _store.Challenges.Last().Stage);
        Assert.DoesNotContain(_store.Sessions, session => session.PlatformUserId == invited.PlatformUserId);
    }

    [Fact]
    public async Task Invite_AboveStandard_NeedsChangeAuthority()
    {
        PlatformUser securityAdmin = AddEnrolledUser("sec@example.com", PlatformUserAuthorityLevel.Administrator);
        PlatformRole role = PlatformRole.CreateSystem(PlatformSystemRoles.SecurityAdministrator, Now);
        role.SynchronizeSystemPermissions(PlatformSystemRoles.Definitions[PlatformSystemRoles.SecurityAdministrator], Now);
        _store.Roles.Add(role);
        securityAdmin.AssignRole(role.Id, Guid.CreateVersion7(Now.AddDays(-1)), null, Now);
        PlatformAuthorizationContext context = new(securityAdmin.Id, PlatformUserAuthorityLevel.Administrator);

        Assert.Equal(
            PlatformAuthErrors.AuthorityNotAllowed,
            (await Invite(context, "boss@example.com", PlatformUserAuthorityLevel.SuperUser)).Error);
        Assert.True((await Invite(context, "staff@example.com", PlatformUserAuthorityLevel.Standard)).Succeeded);
    }

    [Fact]
    public async Task ResetRequest_SameAnswer_ThrottledPerEmail_AndLogged()
    {
        AddEnrolledUser("ada@example.com");

        RequestPasswordResetResponse unknown = await RequestReset("nobody@example.com");
        RequestPasswordResetResponse known = await RequestReset("ada@example.com");
        RequestPasswordResetResponse tooSoon = await RequestReset("ada@example.com");

        Assert.True(unknown.Succeeded && known.Succeeded && tooSoon.Succeeded);
        Assert.Single(_store.SentMail, mail => mail.Kind == "reset");
        Assert.Equal(3, _store.ResetRequests.Count);
    }

    [Fact]
    public async Task ResetPassword_EndsSessions_AndSendsANotice()
    {
        PlatformUser user = AddEnrolledUser("ada@example.com");
        _store.Sessions.Add(PlatformUserSession.Start(user.Id, new string('a', 64), null, null, Now));
        await RequestReset("ada@example.com");
        string token = _store.SentMail.Single(mail => mail.Kind == "reset").Value;

        ResetPasswordResponse response = await new ResetPasswordUseCase(
                _store, _store, _store, _crypto, _crypto, _store, Audit(), _store, _clock)
            .ExecuteAsync(Anonymous, new ResetPasswordRequest { Token = token, NewPassword = "new-orbit-lantern-77" }, CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.True(_store.Sessions.Single().IsEnded);
        Assert.Equal("H:new-orbit-lantern-77", user.Account.PasswordHash);
        Assert.Contains(_store.SentMail, mail => mail.Kind == "notice");
    }

    [Fact]
    public async Task SessionValidator_AcceptsLiveSessions_AndEndsIdleOnes()
    {
        PlatformUser user = AddEnrolledUser("ada@example.com");
        PlatformSecret token = _crypto.NewToken();
        _store.Sessions.Add(PlatformUserSession.Start(user.Id, token.Hash, null, null, Now));
        PlatformSessionValidator validator = new(_store, _store, _crypto, _store, _clock);

        PlatformAuthorizationContext? live = await validator.ValidateAsync(token.Value, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(7));
        PlatformAuthorizationContext? idle = await validator.ValidateAsync(token.Value, CancellationToken.None);

        Assert.Equal(user.Id, live!.PlatformUserId);
        Assert.Equal(_store.Sessions[0].Id, live.SessionId);
        Assert.Null(idle);
        Assert.Equal(PlatformSessionEndReason.IdleTimeout, _store.Sessions[0].EndReason);
    }

    [Fact]
    public async Task SessionValidator_RefusesARestrictedAccount()
    {
        PlatformUser user = AddEnrolledUser("ada@example.com");
        PlatformSecret token = _crypto.NewToken();
        _store.Sessions.Add(PlatformUserSession.Start(user.Id, token.Hash, null, null, Now));
        user.Suspend("review", Guid.CreateVersion7(Now), 1, Now);

        Assert.Null(await new PlatformSessionValidator(_store, _store, _crypto, _store, _clock).ValidateAsync(token.Value, CancellationToken.None));
    }

    [Fact]
    public async Task FirstSuperUserSetup_WorksOnlyOnAnEmptyDatabase()
    {
        PlatformFirstSuperUserSetup setup = new(_store, _store, _crypto, Audit(), _store, _clock);

        (string? token, _) = await setup.RunAsync("Jude", "Owner", "jude@example.com", CancellationToken.None);
        (_, Vixly.Shared.Kernel.Errors.Error? again) = await setup.RunAsync("X", "Y", "x@example.com", CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(PlatformUserAuthorityLevel.SuperUser, _store.Users.Single().AuthorityLevel);
        Assert.Equal(PlatformAuthErrors.SetupAlreadyDone, again);
        Assert.True((await Accept(token!, GoodPassword)).Succeeded);
    }

    private Task<BeginSignInResponse> Begin(string email, string password)
    {
        return new BeginSignInUseCase(_store, _crypto, Steps(), Audit(), _store)
            .ExecuteAsync(Anonymous, new BeginSignInRequest { Email = email, Password = password }, CancellationToken.None);
    }

    private Task<CompleteSignInWithAuthenticatorCodeResponse> Authenticator(string challenge, string code)
    {
        return new CompleteSignInWithAuthenticatorCodeUseCase(Steps(), _crypto)
            .ExecuteAsync(
                Anonymous,
                new CompleteSignInWithAuthenticatorCodeRequest { ChallengeToken = challenge, Code = code },
                CancellationToken.None);
    }

    private Task<CompleteSignInWithRecoveryCodeResponse> Recovery(string challenge, string code)
    {
        return new CompleteSignInWithRecoveryCodeUseCase(Steps(), _crypto)
            .ExecuteAsync(
                Anonymous,
                new CompleteSignInWithRecoveryCodeRequest { ChallengeToken = challenge, Code = code },
                CancellationToken.None);
    }

    private Task<AcceptInvitationResponse> Accept(string token, string password)
    {
        return new AcceptInvitationUseCase(_store, _store, _crypto, _crypto, Steps(), Audit(), _store)
            .ExecuteAsync(Anonymous, new AcceptInvitationRequest { Token = token, Password = password }, CancellationToken.None);
    }

    private Task<RequestPasswordResetResponse> RequestReset(string email)
    {
        return new RequestPasswordResetUseCase(_store, _store, _store, _crypto, _store, _store, Audit(), _store, _clock)
            .ExecuteAsync(Anonymous, new RequestPasswordResetRequest { Email = email }, CancellationToken.None);
    }

    private Task<InvitePlatformUserResponse> Invite(
        PlatformAuthorizationContext context,
        string email,
        PlatformUserAuthorityLevel level)
    {
        PlatformAuthorizationService authorization = new(_store, _clock);

        return new InvitePlatformUserUseCase(
                new PlatformAuthorizationGate(authorization),
                authorization,
                _store,
                _store,
                _crypto,
                _store,
                Audit(),
                _store,
                _clock)
            .ExecuteAsync(
                context,
                new InvitePlatformUserRequest
                {
                    FirstName = "Grace",
                    LastName = "Hopper",
                    Email = email,
                    PreferredLanguage = "fr",
                    AuthorityLevel = level
                },
                CancellationToken.None);
    }

    private PlatformSignInSteps Steps()
    {
        return new PlatformSignInSteps(_store, _store, _store, _crypto, _store, Audit(), _store, _clock);
    }

    private PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(_store, _store, _clock);
    }

    private PlatformAuthorizationContext SuperUserContext()
    {
        PlatformUser admin = AddEnrolledUser("owner@example.com", PlatformUserAuthorityLevel.SuperUser);
        return new PlatformAuthorizationContext(admin.Id, PlatformUserAuthorityLevel.SuperUser);
    }

    private PlatformUser AddActiveUser(string email, string? passwordHash = null)
    {
        PlatformUser user = PlatformUser.Invite(
            "Ada",
            "Lovelace",
            EmailAddress.Create(email),
            LanguageCode.En,
            PlatformUserAuthorityLevel.Standard,
            null,
            null,
            Now);
        user.AcceptInvitation(passwordHash ?? "H:" + GoodPassword, Now);
        _store.Users.Add(user);

        return user;
    }

    private PlatformUser AddEnrolledUser(string email, PlatformUserAuthorityLevel level = PlatformUserAuthorityLevel.Standard)
    {
        PlatformUser user = PlatformUser.Invite(
            "Ada",
            "Lovelace",
            EmailAddress.Create(email),
            LanguageCode.En,
            level,
            null,
            null,
            Now);
        user.AcceptInvitation("H:" + GoodPassword, Now);
        user.BeginAuthenticatorEnrolment("encrypted-secret", Now);
        user.ConfirmAuthenticatorApp(1, Now);
        _store.Users.Add(user);

        return user;
    }
}
