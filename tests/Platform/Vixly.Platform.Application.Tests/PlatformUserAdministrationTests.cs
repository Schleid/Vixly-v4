using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactor;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Application.UseCases.Users.EndPlatformUserSessions;
using Vixly.Platform.Application.UseCases.Users.GetPlatformUser;
using Vixly.Platform.Application.UseCases.Users.ListPlatformUsers;
using Vixly.Platform.Application.UseCases.Users.ResendPlatformUserInvitation;
using Vixly.Platform.Application.UseCases.Users.ResetPlatformUserSecondFactor;
using Vixly.Platform.Application.UseCases.Users.SendPlatformUserPasswordReset;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformUserAdministrationTests.cs
//
// PURPOSE:
// Proves step-up confirmation, the access summary and the
// user administration actions added for the Users screens.
//
// RESPONSIBILITIES:
// - Step-up: a good code opens the window; five wrong codes
//   end the session; a recovery code is spent once
// - Access summary: SuperUser holds all, inactive holds none
// - End sessions, reset second factor, resend invitation,
//   send password reset: effect, audit, mail, refusals
// - List and detail: permission and filter cleaning
//
// IMPORTANT NOTES:
// - Uses the real gate, authorization service and step-up
//   guard over the in-memory store
// =========================================================
public sealed class PlatformUserAdministrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FakeCrypto _crypto = new();

    private readonly FixedTimeProvider _clock = new(Now);

    private int _tokens;

    [Fact]
    public async Task ConfirmSecondFactor_WithAGoodCode_OpensTheStepUpWindow()
    {
        (_, PlatformAuthorizationContext context, PlatformUserSession session) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        _clock.Advance(TimeSpan.FromSeconds(330));
        Assert.False(session.HasRecentSecondFactor(_clock.UtcNow));

        ConfirmSecondFactorResponse response = await Confirm().ExecuteAsync(
            context,
            new ConfirmSecondFactorRequest { Code = FakeCrypto.ValidCode },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.True(session.HasRecentSecondFactor(_clock.UtcNow));
        Assert.Equal("platform.auth.step_up_confirmed", _store.AuditEntries.Single().ActionCode);
    }

    [Fact]
    public async Task ConfirmSecondFactor_FiveWrongCodes_EndTheSession()
    {
        (_, PlatformAuthorizationContext context, PlatformUserSession session) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        ConfirmSecondFactorRequest wrong = new() { Code = "000000" };

        for (int attempt = 0; attempt < 4; attempt++)
        {
            Assert.Equal(PlatformAuthErrors.CodeInvalid, (await Confirm().ExecuteAsync(context, wrong, CancellationToken.None)).Error);
        }

        Assert.Equal(PlatformAuthErrors.SessionEnded, (await Confirm().ExecuteAsync(context, wrong, CancellationToken.None)).Error);
        Assert.Equal(PlatformSessionEndReason.StepUpFailed, session.EndReason);
        Assert.Equal(5, _store.AuditEntries.Count(entry => entry.ActionCode == "platform.auth.step_up_failed"));
        Assert.Equal(
            PlatformAuthErrors.SessionEnded,
            (await Confirm().ExecuteAsync(context, new ConfirmSecondFactorRequest { Code = FakeCrypto.ValidCode }, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task ConfirmSecondFactor_RecoveryCode_WorksOnce()
    {
        (PlatformUser user, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        List<string> codes = Enumerable.Range(0, 10).Select(index => $"RC{index:D8}").ToList();
        user.ReplaceRecoveryCodes(codes.Select(_crypto.HashRecoveryCode).ToList(), Now);
        ConfirmSecondFactorRequest request = new() { Code = codes[3].ToLowerInvariant(), UseRecoveryCode = true };

        Assert.True((await Confirm().ExecuteAsync(context, request, CancellationToken.None)).Succeeded);
        Assert.Equal(9, user.UnusedRecoveryCodeCount);
        Assert.Equal(PlatformAuthErrors.CodeInvalid, (await Confirm().ExecuteAsync(context, request, CancellationToken.None)).Error);
    }

    [Fact]
    public async Task GetMyPlatformAccess_SuperUserHoldsAll_StandardHoldsNone()
    {
        (_, PlatformAuthorizationContext superUser, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (PlatformUser standardUser, PlatformAuthorizationContext standard, _) = SignedIn(PlatformUserAuthorityLevel.Standard);

        GetMyPlatformAccessResponse all = await Access().ExecuteAsync(superUser, new GetMyPlatformAccessRequest(), CancellationToken.None);
        GetMyPlatformAccessResponse none = await Access().ExecuteAsync(standard, new GetMyPlatformAccessRequest(), CancellationToken.None);

        Assert.True(all.IsSuperUser);
        Assert.Equal(PlatformPermissions.All.Count, all.PermissionCodes.Count);
        Assert.Empty(none.PermissionCodes);

        standardUser.Lock("test", superUser.PlatformUserId, 1, Now);
        Assert.Empty((await Access().ExecuteAsync(standard, new GetMyPlatformAccessRequest(), CancellationToken.None)).PermissionCodes);
    }

    [Fact]
    public async Task EndSessions_EndsTheTargetsSessions_ButNotYourOwn()
    {
        (_, PlatformAuthorizationContext context, PlatformUserSession mine) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (PlatformUser target, _, PlatformUserSession theirs) = SignedIn(PlatformUserAuthorityLevel.Standard);

        EndPlatformUserSessionsResponse response = await EndSessions().ExecuteAsync(
            context,
            new EndPlatformUserSessionsRequest { PlatformUserId = target.Id },
            CancellationToken.None);

        Assert.Equal(1, response.EndedCount);
        Assert.Equal(PlatformSessionEndReason.EndedByAdministrator, theirs.EndReason);
        Assert.False(mine.IsEnded);
        Assert.Equal("platform.users.sessions_ended", _store.AuditEntries.Single().ActionCode);

        EndPlatformUserSessionsResponse self = await EndSessions().ExecuteAsync(
            context,
            new EndPlatformUserSessionsRequest { PlatformUserId = context.PlatformUserId },
            CancellationToken.None);

        Assert.Equal(PlatformIdentityErrors.CannotActOnOwnAccount, self.Error);
    }

    [Fact]
    public async Task ResetSecondFactor_ClearsFactors_EndsSessions_AndSendsANotice()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (PlatformUser target, _, PlatformUserSession theirs) = SignedIn(PlatformUserAuthorityLevel.Standard);

        ResetPlatformUserSecondFactorResponse response = await ResetSecondFactor().ExecuteAsync(
            context,
            new ResetPlatformUserSecondFactorRequest { PlatformUserId = target.Id, Reason = "lost phone" },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.False(target.HasSecondFactor);
        Assert.Equal(PlatformSessionEndReason.SecondFactorReset, theirs.EndReason);
        Assert.Equal(("notice", target.Id, "platform.notice.second_factor_reset"), _store.SentMail.Single());
        Assert.Equal("lost phone", _store.AuditEntries.Single().Reason);
    }

    [Fact]
    public async Task ResetSecondFactor_NeedsAReason_AndAStepUp()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (PlatformUser target, _, _) = SignedIn(PlatformUserAuthorityLevel.Standard);

        ResetPlatformUserSecondFactorResponse noReason = await ResetSecondFactor().ExecuteAsync(
            context,
            new ResetPlatformUserSecondFactorRequest { PlatformUserId = target.Id, Reason = " " },
            CancellationToken.None);

        _clock.Advance(TimeSpan.FromMinutes(6));

        ResetPlatformUserSecondFactorResponse stale = await ResetSecondFactor().ExecuteAsync(
            context,
            new ResetPlatformUserSecondFactorRequest { PlatformUserId = target.Id, Reason = "lost phone" },
            CancellationToken.None);

        Assert.Equal(PlatformIdentityErrors.ReasonRequired, noReason.Error);
        Assert.Equal(PlatformCommonErrors.StepUpRequired, stale.Error);
        Assert.True(target.HasSecondFactor);
        Assert.Equal(0, _store.Saves);
    }

    [Fact]
    public async Task ResendInvitation_RevokesTheOldLink_AndSendsANewOne()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        PlatformUser invited = Invited();
        PlatformAccountToken first = PlatformAccountToken.Issue(invited.Id, PlatformAccountTokenPurpose.Invitation, new string('e', 64), Now);
        _store.AccountTokens.Add(first);

        ResendPlatformUserInvitationResponse response = await ResendInvitation().ExecuteAsync(
            context,
            new ResendPlatformUserInvitationRequest { PlatformUserId = invited.Id },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.NotNull(first.RevokedUtc);
        Assert.Equal(2, _store.AccountTokens.Count);
        Assert.Equal("invitation", _store.SentMail.Single().Kind);
        Assert.Equal("platform.users.invitation_resent", _store.AuditEntries.Single().ActionCode);
    }

    [Fact]
    public async Task ResendInvitation_ToAnActiveAccount_IsRefused()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (PlatformUser active, _, _) = SignedIn(PlatformUserAuthorityLevel.Standard);

        ResendPlatformUserInvitationResponse response = await ResendInvitation().ExecuteAsync(
            context,
            new ResendPlatformUserInvitationRequest { PlatformUserId = active.Id },
            CancellationToken.None);

        Assert.Equal(PlatformAuthErrors.AccountNotInvited, response.Error);
        Assert.Empty(_store.SentMail);
    }

    [Fact]
    public async Task SendPasswordReset_WorksForActive_NotForInvited()
    {
        (_, PlatformAuthorizationContext context, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (PlatformUser active, _, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        PlatformUser invited = Invited();

        SendPlatformUserPasswordResetResponse sent = await SendReset().ExecuteAsync(
            context,
            new SendPlatformUserPasswordResetRequest { PlatformUserId = active.Id },
            CancellationToken.None);
        SendPlatformUserPasswordResetResponse refused = await SendReset().ExecuteAsync(
            context,
            new SendPlatformUserPasswordResetRequest { PlatformUserId = invited.Id },
            CancellationToken.None);

        Assert.True(sent.Succeeded);
        Assert.Equal(("reset", active.Id), (_store.SentMail.Single().Kind, _store.SentMail.Single().UserId));
        Assert.Equal(PlatformAccountTokenPurpose.PasswordReset, _store.AccountTokens.Single().Purpose);
        Assert.Equal(PlatformIdentityErrors.AccountNotActive, refused.Error);
    }

    [Fact]
    public async Task ListAndGet_NeedTheViewPermission_AndCleanTheSearch()
    {
        (_, PlatformAuthorizationContext superUser, _) = SignedIn(PlatformUserAuthorityLevel.SuperUser);
        (_, PlatformAuthorizationContext standard, _) = SignedIn(PlatformUserAuthorityLevel.Standard);
        ListPlatformUsersUseCase list = new(Gate(), _store);

        ListPlatformUsersResponse response = await list.ExecuteAsync(
            superUser,
            new ListPlatformUsersRequest { Search = "  " + new string('x', 150) + " ", PageSize = 1000 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(100, _store.LastListFilter!.Search!.Length);
        Assert.Equal(100, response.Users!.PageSize);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            list.ExecuteAsync(standard, new ListPlatformUsersRequest(), CancellationToken.None));

        GetPlatformUserResponse missing = await new GetPlatformUserUseCase(Gate(), _store, _clock).ExecuteAsync(
            superUser,
            new GetPlatformUserRequest { PlatformUserId = Guid.CreateVersion7(Now) },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.NotFound, missing.Error);
    }

    private ConfirmSecondFactorUseCase Confirm()
    {
        return new ConfirmSecondFactorUseCase(new PlatformStepUpSteps(_store, _store, Audit(), _store, _clock), _crypto, _crypto);
    }

    private GetMyPlatformAccessUseCase Access()
    {
        return new GetMyPlatformAccessUseCase(_store, _clock);
    }

    private EndPlatformUserSessionsUseCase EndSessions()
    {
        return new EndPlatformUserSessionsUseCase(Administration(), _store, Audit(), _store, _clock);
    }

    private ResetPlatformUserSecondFactorUseCase ResetSecondFactor()
    {
        return new ResetPlatformUserSecondFactorUseCase(Administration(), _store, _store, Audit(), _store, _clock);
    }

    private ResendPlatformUserInvitationUseCase ResendInvitation()
    {
        return new ResendPlatformUserInvitationUseCase(Administration(), _store, _crypto, _store, Audit(), _store, _clock);
    }

    private SendPlatformUserPasswordResetUseCase SendReset()
    {
        return new SendPlatformUserPasswordResetUseCase(Administration(), _store, _crypto, _store, Audit(), _store, _clock);
    }

    private PlatformUserAdministration Administration()
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

    private PlatformUser Invited()
    {
        return NewUser(PlatformUserAuthorityLevel.Standard);
    }

    private PlatformUser NewUser(PlatformUserAuthorityLevel level)
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
        _store.Users.Add(user);

        return user;
    }

    private string NextTokenHash()
    {
        return (++_tokens).ToString("D64", System.Globalization.CultureInfo.InvariantCulture);
    }
}
