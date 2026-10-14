using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyRegistration;
using Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyStepUp;
using Vixly.Platform.Application.UseCases.MyAccount.CompletePasskeyRegistration;
using Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactorWithPasskey;
using Vixly.Platform.Application.UseCases.MyAccount.RemoveMyPasskey;
using Vixly.Platform.Application.UseCases.SignIn.BeginPasskeySignIn;
using Vixly.Platform.Application.UseCases.SignIn.CompletePasskeySignIn;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformPasskeyTests.cs
//
// PURPOSE:
// Proves the passkey use cases over the in-memory store
// (ADR-0006 section 2).
//
// RESPONSIBILITIES:
// - Adding a passkey: step-up first, single-use ceremony, no
//   passkey of another account, audit and notice
// - Removing a passkey: step-up, notice
// - Signing in with a passkey: a session for a verified answer;
//   the same refusal for unknown passkeys, blocked accounts and
//   bad answers; no replay
// - Step-up with a passkey: opens the window; a passkey of
//   another account counts as a failure
//
// IMPORTANT NOTES:
// - WebAuthn itself is faked (FakeCrypto); the real adapter is
//   proven in the browser run
// =========================================================
public sealed class PlatformPasskeyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private const string CredentialHex = "0A0B0C";

    private readonly FakePlatformStore _store = new();

    private readonly FakeCrypto _crypto = new();

    private readonly FixedTimeProvider _clock = new(Now);

    private int _tokens;

    [Fact]
    public async Task AddPasskey_NeedsAStepUp_ThenRegistersOnce_AndSendsANotice()
    {
        (PlatformUser user, PlatformAuthorizationContext context, _) = SignedIn();
        _clock.Advance(TimeSpan.FromSeconds(330));

        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await BeginRegistration().ExecuteAsync(context, new BeginPasskeyRegistrationRequest(), CancellationToken.None)).Error);

        _store.Sessions.Single(session => session.Id == context.SessionId).ConfirmSecondFactor(_clock.UtcNow);
        BeginPasskeyRegistrationResponse begun = await BeginRegistration().ExecuteAsync(context, new BeginPasskeyRegistrationRequest(), CancellationToken.None);
        CompletePasskeyRegistrationRequest answer = new() { CeremonyToken = begun.CeremonyToken, AttestationJson = "good:" + CredentialHex, Name = "Work laptop" };

        CompletePasskeyRegistrationResponse added = await CompleteRegistration().ExecuteAsync(context, answer, CancellationToken.None);
        CompletePasskeyRegistrationResponse replay = await CompleteRegistration().ExecuteAsync(context, answer, CancellationToken.None);

        Assert.True(added.Succeeded);
        Assert.Equal("Work laptop", user.Passkeys.Single().Name);
        Assert.Equal(PlatformIdentityErrors.PasskeyCeremonyInvalid, replay.Error);
        Assert.Equal(("notice", user.Id, "platform.notice.passkey_added"), _store.SentMail.Single());
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.account.passkey_added");
    }

    [Fact]
    public async Task AddPasskey_AlreadyOnAnotherAccount_IsRefused()
    {
        (PlatformUser other, _, _) = SignedIn();
        other.RegisterPasskey(Convert.FromHexString(CredentialHex), new byte[] { 9 }, 0, Guid.Empty, "Phone", Now);
        (_, PlatformAuthorizationContext context, _) = SignedIn();

        BeginPasskeyRegistrationResponse begun = await BeginRegistration().ExecuteAsync(context, new BeginPasskeyRegistrationRequest(), CancellationToken.None);
        CompletePasskeyRegistrationResponse response = await CompleteRegistration().ExecuteAsync(
            context,
            new CompletePasskeyRegistrationRequest { CeremonyToken = begun.CeremonyToken, AttestationJson = "good:" + CredentialHex, Name = "Mine" },
            CancellationToken.None);

        Assert.Equal(PlatformIdentityErrors.PasskeyAlreadyRegistered, response.Error);
        Assert.Single(other.Passkeys);
    }

    [Fact]
    public async Task SignInWithPasskey_OpensASession_AndCannotBeReplayed()
    {
        (PlatformUser user, _, _) = SignedIn();
        user.RegisterPasskey(Convert.FromHexString(CredentialHex), new byte[] { 9 }, 0, Guid.Empty, "Phone", Now);
        BeginPasskeySignInResponse begun = await new BeginPasskeySignInUseCase(_crypto, Ceremonies(), _store)
            .ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new BeginPasskeySignInRequest(), CancellationToken.None);
        CompletePasskeySignInRequest answer = new() { CeremonyToken = begun.CeremonyToken, AssertionJson = $"good:{CredentialHex}:1" };

        CompletePasskeySignInResponse first = await CompleteSignIn().ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, answer, CancellationToken.None);
        CompletePasskeySignInResponse replay = await CompleteSignIn().ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, answer, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.Contains(_store.Sessions, session => session.Id == first.SessionId && session.HasRecentSecondFactor(Now));
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.signed_in" && entry.ArgumentsJson!.Contains("passkey", StringComparison.Ordinal));
        Assert.Equal(PlatformAuthErrors.SignInExpired, replay.Error);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("bad")]
    [InlineData("locked")]
    public async Task SignInWithPasskey_RefusesWithTheSameAnswer(string scenario)
    {
        (PlatformUser user, _, _) = SignedIn();
        user.RegisterPasskey(Convert.FromHexString(CredentialHex), new byte[] { 9 }, 0, Guid.Empty, "Phone", Now);

        if (scenario == "locked")
        {
            user.Lock("left", Guid.CreateVersion7(Now), 1, Now);
        }

        string assertion = scenario switch
        {
            "unknown" => "good:FFFF:1",
            "bad" => "bad:" + CredentialHex,
            _ => $"good:{CredentialHex}:1"
        };
        BeginPasskeySignInResponse begun = await new BeginPasskeySignInUseCase(_crypto, Ceremonies(), _store)
            .ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new BeginPasskeySignInRequest(), CancellationToken.None);

        CompletePasskeySignInResponse response = await CompleteSignIn().ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new CompletePasskeySignInRequest { CeremonyToken = begun.CeremonyToken, AssertionJson = assertion },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.SignInFailed, response.Error);
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.passkey_failed");
        Assert.Single(_store.Sessions);
    }

    [Fact]
    public async Task StepUpWithPasskey_OpensTheWindow_AndRefusesAnotherAccountsPasskey()
    {
        (PlatformUser other, _, _) = SignedIn();
        other.RegisterPasskey(new byte[] { 7, 7 }, new byte[] { 9 }, 0, Guid.Empty, "Theirs", Now);
        (PlatformUser user, PlatformAuthorizationContext context, PlatformUserSession session) = SignedIn();
        user.RegisterPasskey(Convert.FromHexString(CredentialHex), new byte[] { 9 }, 0, Guid.Empty, "Mine", Now);
        _clock.Advance(TimeSpan.FromSeconds(330));

        BeginPasskeyStepUpResponse wrongBegin = await BeginStepUp().ExecuteAsync(context, new BeginPasskeyStepUpRequest(), CancellationToken.None);
        ConfirmSecondFactorWithPasskeyResponse wrong = await ConfirmStepUp().ExecuteAsync(
            context,
            new ConfirmSecondFactorWithPasskeyRequest { CeremonyToken = wrongBegin.CeremonyToken, AssertionJson = "good:0707:1" },
            CancellationToken.None);

        BeginPasskeyStepUpResponse begun = await BeginStepUp().ExecuteAsync(context, new BeginPasskeyStepUpRequest(), CancellationToken.None);
        ConfirmSecondFactorWithPasskeyResponse right = await ConfirmStepUp().ExecuteAsync(
            context,
            new ConfirmSecondFactorWithPasskeyRequest { CeremonyToken = begun.CeremonyToken, AssertionJson = $"good:{CredentialHex}:3" },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.PasskeyNotAccepted, wrong.Error);
        Assert.True(right.Succeeded);
        Assert.True(session.HasRecentSecondFactor(_clock.UtcNow));
        Assert.Equal(3, user.Passkeys.Single().SignatureCounter);
    }

    [Fact]
    public async Task StepUpWithPasskey_WithoutPasskeys_SaysSo()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn();

        Assert.Equal(PlatformAuthErrors.NoPasskeys, (await BeginStepUp().ExecuteAsync(context, new BeginPasskeyStepUpRequest(), CancellationToken.None)).Error);
    }

    [Fact]
    public async Task RemovePasskey_WithAStepUp_SendsANotice()
    {
        (PlatformUser user, PlatformAuthorizationContext context, _) = SignedIn();
        PlatformUserPasskey passkey = user.RegisterPasskey(Convert.FromHexString(CredentialHex), new byte[] { 9 }, 0, Guid.Empty, "Old phone", Now);

        RemoveMyPasskeyResponse response = await new RemoveMyPasskeyUseCase(new PlatformStepUpGuard(_store, _clock), _store, _store, Audit(), _store, _clock)
            .ExecuteAsync(context, new RemoveMyPasskeyRequest { PasskeyId = passkey.Id }, CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Empty(user.Passkeys);
        Assert.Equal("platform.notice.passkey_removed", _store.SentMail.Single().Value);
    }

    private BeginPasskeyRegistrationUseCase BeginRegistration()
    {
        return new BeginPasskeyRegistrationUseCase(new PlatformStepUpGuard(_store, _clock), _store, _crypto, Ceremonies(), _store);
    }

    private CompletePasskeyRegistrationUseCase CompleteRegistration()
    {
        return new CompletePasskeyRegistrationUseCase(Ceremonies(), _crypto, _store, _store, Audit(), _store, _clock);
    }

    private CompletePasskeySignInUseCase CompleteSignIn()
    {
        return new CompletePasskeySignInUseCase(
            Ceremonies(),
            _crypto,
            _store,
            new PlatformSignInSteps(_store, _store, _store, _crypto, _store, Audit(), _store, _clock),
            Audit(),
            _store);
    }

    private BeginPasskeyStepUpUseCase BeginStepUp()
    {
        return new BeginPasskeyStepUpUseCase(StepUpSteps(), _crypto, Ceremonies(), _store);
    }

    private ConfirmSecondFactorWithPasskeyUseCase ConfirmStepUp()
    {
        return new ConfirmSecondFactorWithPasskeyUseCase(StepUpSteps(), Ceremonies(), _crypto);
    }

    private PlatformStepUpSteps StepUpSteps()
    {
        return new PlatformStepUpSteps(_store, _store, Audit(), _store, _clock);
    }

    private PlatformPasskeyCeremonies Ceremonies()
    {
        return new PlatformPasskeyCeremonies(_store, _crypto, _clock);
    }

    private PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(_store, _store, _clock);
    }

    private (PlatformUser User, PlatformAuthorizationContext Context, PlatformUserSession Session) SignedIn()
    {
        PlatformUser user = PlatformUser.Invite(
            "Test",
            "User",
            EmailAddress.Create($"p{_store.Users.Count}@example.com"),
            LanguageCode.En,
            PlatformUserAuthorityLevel.Standard,
            null,
            null,
            Now);
        user.AcceptInvitation("H:password", Now);
        user.BeginAuthenticatorEnrolment("encrypted-secret", Now);
        user.ConfirmAuthenticatorApp(1, Now);
        _store.Users.Add(user);

        PlatformUserSession session = PlatformUserSession.Start(user.Id, (++_tokens).ToString("D64", System.Globalization.CultureInfo.InvariantCulture), null, null, Now);
        _store.Sessions.Add(session);

        return (user, new PlatformAuthorizationContext(user.Id, PlatformUserAuthorityLevel.Standard, session.Id), session);
    }
}
