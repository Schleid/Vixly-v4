using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.SignIn.BeginPasskeyEnrolment;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;
using Vixly.Platform.Application.UseCases.SignIn.BeginSignInEnrolment;
using Vixly.Platform.Application.UseCases.SignIn.CompletePasskeyEnrolment;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformPasskeyEnrolmentTests.cs
//
// PURPOSE:
// Proves that a passkey can be the first second factor, set up
// during the first sign-in.
//
// RESPONSIBILITIES:
// - A good passkey opens the session with 10 recovery codes,
//   drops the unconfirmed authenticator app and sends a notice
// - A refused passkey counts as a failed attempt and opens
//   nothing
// - The ceremony works only with the enrolment challenge of the
//   same user
//
// IMPORTANT NOTES:
// - FakeCrypto accepts the attestation "good:<hex id>"
// =========================================================
public sealed class PlatformPasskeyEnrolmentTests
{
    private const string GoodPassword = "orbit-lantern-quarry-42";

    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static PlatformAnonymousAuthorizationContext Anonymous => PlatformAnonymousAuthorizationContext.Instance;

    private readonly FakePlatformStore _store = new();

    private readonly FakeCrypto _crypto = new();

    private readonly FixedTimeProvider _clock = new(Now);

    [Fact]
    public async Task GoodPasskey_OpensTheSession_WithRecoveryCodes()
    {
        PlatformUser user = AddActiveUser("ada@example.com");
        string challenge = await StartEnrolmentAsync("ada@example.com");
        await new BeginSignInEnrolmentUseCase(Steps(), _crypto, _store)
            .ExecuteAsync(Anonymous, new BeginSignInEnrolmentRequest { ChallengeToken = challenge }, CancellationToken.None);

        BeginPasskeyEnrolmentResponse options = await Begin().ExecuteAsync(Anonymous, new BeginPasskeyEnrolmentRequest { ChallengeToken = challenge }, CancellationToken.None);
        CompletePasskeyEnrolmentResponse done = await Complete().ExecuteAsync(
            Anonymous,
            new CompletePasskeyEnrolmentRequest { ChallengeToken = challenge, CeremonyToken = options.CeremonyToken, AttestationJson = "good:0A0B", Name = "First passkey" },
            CancellationToken.None);

        Assert.True(done.Succeeded);
        Assert.Equal(10, done.RecoveryCodes.Count);
        Assert.Single(user.Passkeys);
        Assert.Null(user.AuthenticatorApp);
        Assert.True(_store.Sessions.Single().HasRecentSecondFactor(Now));
        Assert.Contains(_store.SentMail, mail => mail.Value == "platform.notice.passkey_added");
        Assert.Contains(_store.AuditEntries, entry => entry.ActionCode == "platform.auth.signed_in");
    }

    [Fact]
    public async Task RefusedPasskey_CountsAsAFailedAttempt()
    {
        PlatformUser user = AddActiveUser("ada@example.com");
        string challenge = await StartEnrolmentAsync("ada@example.com");

        BeginPasskeyEnrolmentResponse options = await Begin().ExecuteAsync(Anonymous, new BeginPasskeyEnrolmentRequest { ChallengeToken = challenge }, CancellationToken.None);
        CompletePasskeyEnrolmentResponse refused = await Complete().ExecuteAsync(
            Anonymous,
            new CompletePasskeyEnrolmentRequest { ChallengeToken = challenge, CeremonyToken = options.CeremonyToken, AttestationJson = "bad", Name = "First passkey" },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.PasskeyNotAccepted, refused.Error);
        Assert.Empty(user.Passkeys);
        Assert.Empty(_store.Sessions);
        Assert.Equal(1, _store.Challenges.Single().FailedAttempts);
    }

    [Fact]
    public async Task Ceremony_OfAnotherUser_IsRefused()
    {
        AddActiveUser("ada@example.com");
        AddActiveUser("bob@example.com");
        string ada = await StartEnrolmentAsync("ada@example.com");
        string bob = await StartEnrolmentAsync("bob@example.com");

        BeginPasskeyEnrolmentResponse adaOptions = await Begin().ExecuteAsync(Anonymous, new BeginPasskeyEnrolmentRequest { ChallengeToken = ada }, CancellationToken.None);
        CompletePasskeyEnrolmentResponse crossed = await Complete().ExecuteAsync(
            Anonymous,
            new CompletePasskeyEnrolmentRequest { ChallengeToken = bob, CeremonyToken = adaOptions.CeremonyToken, AttestationJson = "good:0C0D", Name = "First passkey" },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.PasskeyNotAccepted, crossed.Error);
        Assert.Empty(_store.Sessions);
    }

    [Fact]
    public async Task WithoutAnEnrolmentChallenge_NothingStarts()
    {
        BeginPasskeyEnrolmentResponse response = await Begin().ExecuteAsync(Anonymous, new BeginPasskeyEnrolmentRequest { ChallengeToken = "nope" }, CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.SignInExpired, response.Error);
        Assert.Empty(_store.Ceremonies);
    }

    private async Task<string> StartEnrolmentAsync(string email)
    {
        BeginSignInResponse begin = await new BeginSignInUseCase(_store, _crypto, Steps(), Audit(), _store)
            .ExecuteAsync(Anonymous, new BeginSignInRequest { Email = email, Password = GoodPassword }, CancellationToken.None);
        Assert.True(begin.EnrolmentRequired);

        return begin.ChallengeToken;
    }

    private BeginPasskeyEnrolmentUseCase Begin()
    {
        return new BeginPasskeyEnrolmentUseCase(Steps(), _crypto, Ceremonies(), _store);
    }

    private CompletePasskeyEnrolmentUseCase Complete()
    {
        return new CompletePasskeyEnrolmentUseCase(Steps(), Ceremonies(), _crypto, _store, _crypto, _store, Audit());
    }

    private PlatformSignInSteps Steps()
    {
        return new PlatformSignInSteps(_store, _store, _store, _crypto, _store, Audit(), _store, _clock);
    }

    private PlatformPasskeyCeremonies Ceremonies()
    {
        return new PlatformPasskeyCeremonies(_store, _crypto, _clock);
    }

    private PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(_store, _store, _clock);
    }

    private PlatformUser AddActiveUser(string email)
    {
        PlatformUser user = PlatformUser.Invite("Ada", "Lovelace", EmailAddress.Create(email), LanguageCode.En, PlatformUserAuthorityLevel.Standard, null, null, Now);
        user.AcceptInvitation("H:" + GoodPassword, Now);
        _store.Users.Add(user);

        return user;
    }
}
