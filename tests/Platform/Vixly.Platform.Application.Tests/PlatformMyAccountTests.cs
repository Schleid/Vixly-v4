using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.MyAccount.ChangeMyLanguage;
using Vixly.Platform.Application.UseCases.MyAccount.ChangeMyPassword;
using Vixly.Platform.Application.UseCases.MyAccount.EndMySessions;
using Vixly.Platform.Application.UseCases.MyAccount.RegenerateRecoveryCodes;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformMyAccountTests.cs
//
// PURPOSE:
// Proves the My account use cases.
//
// RESPONSIBILITIES:
// - Change password: current password checked and counted, policy
//   applied, other sessions ended, this one kept, notice sent
// - End sessions: one or all others, never this one, never
//   another user's
// - Recovery codes: step-up first, 10 new codes, old ones dead
// - Language: saved when supported, refused otherwise
//
// IMPORTANT NOTES:
// - Uses the in-memory store and deterministic crypto
// =========================================================
public sealed class PlatformMyAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private const string Password = "orbit-lantern-quarry-42";

    private readonly FakePlatformStore _store = new();

    private readonly FakeCrypto _crypto = new();

    private readonly FixedTimeProvider _clock = new(Now);

    private int _tokens;

    [Fact]
    public async Task ChangePassword_KeepsThisSession_EndsTheOthers_AndSendsANotice()
    {
        (PlatformUser user, PlatformAuthorizationContext context, PlatformUserSession current) = SignedIn();
        PlatformUserSession other = AddSession(user);

        ChangeMyPasswordResponse response = await ChangePassword().ExecuteAsync(
            context,
            new ChangeMyPasswordRequest { CurrentPassword = Password, NewPassword = "velvet-signal-copper-58" },
            CancellationToken.None);

        Assert.Equal(1, response.OtherSessionsEnded);
        Assert.Equal("H:velvet-signal-copper-58", user.Account.PasswordHash);
        Assert.False(current.IsEnded);
        Assert.Equal(PlatformSessionEndReason.PasswordChanged, other.EndReason);
        Assert.Equal("platform.notice.password_changed", _store.SentMail.Single().Value);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_CountsLikeAWrongCode_AndWeakIsRefused()
    {
        (PlatformUser user, PlatformAuthorizationContext context, PlatformUserSession current) = SignedIn();

        ChangeMyPasswordResponse wrong = await ChangePassword().ExecuteAsync(
            context,
            new ChangeMyPasswordRequest { CurrentPassword = "nope", NewPassword = "velvet-signal-copper-58" },
            CancellationToken.None);
        ChangeMyPasswordResponse weak = await ChangePassword().ExecuteAsync(
            context,
            new ChangeMyPasswordRequest { CurrentPassword = Password, NewPassword = "short" },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.PasswordIncorrect, wrong.Error);
        Assert.Equal(1, current.FailedStepUpCount);
        Assert.Equal("platform.auth.password_too_short", weak.Error!.Code);
        Assert.Equal("H:" + Password, user.Account.PasswordHash);
    }

    [Fact]
    public async Task EndSessions_EndsOneOrAllOthers_NeverThisOne_NorSomeoneElses()
    {
        (PlatformUser user, PlatformAuthorizationContext context, PlatformUserSession current) = SignedIn();
        PlatformUserSession first = AddSession(user);
        PlatformUserSession second = AddSession(user);
        (_, _, PlatformUserSession stranger) = SignedIn();

        Assert.Equal(PlatformAuthErrors.CurrentSessionNotEnded, (await EndSessions().ExecuteAsync(context, new EndMySessionsRequest { SessionId = current.Id }, CancellationToken.None)).Error);
        Assert.Equal(PlatformCommonErrors.NotFound, (await EndSessions().ExecuteAsync(context, new EndMySessionsRequest { SessionId = stranger.Id }, CancellationToken.None)).Error);
        Assert.Equal(1, (await EndSessions().ExecuteAsync(context, new EndMySessionsRequest { SessionId = first.Id }, CancellationToken.None)).EndedCount);
        Assert.Equal(1, (await EndSessions().ExecuteAsync(context, new EndMySessionsRequest(), CancellationToken.None)).EndedCount);

        Assert.True(first.IsEnded);
        Assert.True(second.IsEnded);
        Assert.False(current.IsEnded);
        Assert.False(stranger.IsEnded);
    }

    [Fact]
    public async Task RecoveryCodes_NeedAStepUp_ThenReplaceTheOldOnes()
    {
        (PlatformUser user, PlatformAuthorizationContext context, PlatformUserSession current) = SignedIn();
        user.ReplaceRecoveryCodes(Enumerable.Range(0, 10).Select(index => _crypto.HashRecoveryCode($"OLD{index:D5}")).ToList(), Now);
        _clock.Advance(TimeSpan.FromSeconds(330));

        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await Regenerate().ExecuteAsync(context, new RegenerateRecoveryCodesRequest(), CancellationToken.None)).Error);

        current.ConfirmSecondFactor(_clock.UtcNow);
        RegenerateRecoveryCodesResponse response = await Regenerate().ExecuteAsync(context, new RegenerateRecoveryCodesRequest(), CancellationToken.None);

        Assert.Equal(10, response.Codes.Count);
        Assert.Equal(10, user.UnusedRecoveryCodeCount);
        Assert.Throws<DomainException>(() => user.UseRecoveryCode(_crypto.HashRecoveryCode("OLD00001"), _clock.UtcNow));
        user.UseRecoveryCode(_crypto.HashRecoveryCode(response.Codes[0]), _clock.UtcNow);
        Assert.Equal("platform.notice.recovery_codes_replaced", _store.SentMail.Single().Value);
    }

    [Fact]
    public async Task Language_IsSaved_WhenSupported()
    {
        (PlatformUser user, PlatformAuthorizationContext context, _) = SignedIn();
        ChangeMyLanguageUseCase useCase = new(_store, Audit(), _store, _clock);

        Assert.True((await useCase.ExecuteAsync(context, new ChangeMyLanguageRequest { Language = "fr" }, CancellationToken.None)).Succeeded);
        Error? refused = (await useCase.ExecuteAsync(context, new ChangeMyLanguageRequest { Language = "de" }, CancellationToken.None)).Error;

        Assert.Equal("fr", user.PreferredLanguage.Value);
        Assert.Equal("shared.language.unsupported", refused!.Code);
    }

    private ChangeMyPasswordUseCase ChangePassword()
    {
        return new ChangeMyPasswordUseCase(new PlatformStepUpSteps(_store, _store, Audit(), _store, _clock), _crypto, _store, _store, Audit(), _store);
    }

    private EndMySessionsUseCase EndSessions()
    {
        return new EndMySessionsUseCase(_store, Audit(), _store, _clock);
    }

    private RegenerateRecoveryCodesUseCase Regenerate()
    {
        return new RegenerateRecoveryCodesUseCase(new PlatformStepUpGuard(_store, _clock), _store, _crypto, _store, Audit(), _store, _clock);
    }

    private PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(_store, _store, _clock);
    }

    private PlatformUserSession AddSession(PlatformUser user)
    {
        PlatformUserSession session = PlatformUserSession.Start(user.Id, (++_tokens).ToString("D64", System.Globalization.CultureInfo.InvariantCulture), null, null, Now);
        _store.Sessions.Add(session);

        return session;
    }

    private (PlatformUser User, PlatformAuthorizationContext Context, PlatformUserSession Session) SignedIn()
    {
        PlatformUser user = PlatformUser.Invite(
            "Test", "User", EmailAddress.Create($"m{_store.Users.Count}@example.com"), LanguageCode.En,
            PlatformUserAuthorityLevel.Standard, null, null, Now);
        user.AcceptInvitation("H:" + Password, Now);
        _store.Users.Add(user);
        PlatformUserSession session = AddSession(user);

        return (user, new PlatformAuthorizationContext(user.Id, PlatformUserAuthorityLevel.Standard, session.Id), session);
    }
}
