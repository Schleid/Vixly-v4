using Vixly.Platform.Domain.Audit;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformSessionAndTokenTests.cs
//
// PURPOSE:
// Proves sessions, one-time account links and audit entries.
//
// RESPONSIBILITIES:
// - Sessions: idle timeout, absolute lifetime, step-up
//   window and failure limit, ending
// - Account links: lifetimes by purpose, single use, revoke
// - Audit entries: validated codes, clipped free text
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformSessionAndTokenTests
{
    private static readonly Guid User = Guid.CreateVersion7(Now);

    private static readonly string TokenHash = new('a', 64);

    [Fact]
    public void Session_ExpiresWhenIdle_AndKeepsAliveWhenTouched()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, "203.0.113.5", "Edge", Now);

        Assert.True(session.IsValidAt(Now.AddMinutes(5)));
        Assert.True(session.Touch(Now.AddMinutes(5)));
        Assert.True(session.IsValidAt(Now.AddMinutes(10)));
        Assert.Equal(PlatformSessionEndReason.IdleTimeout, session.ExpiryReasonAt(Now.AddMinutes(11)));
    }

    [Fact]
    public void Session_TouchWritesAtMostOncePerMinute()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, null, Now);

        Assert.False(session.Touch(Now.AddSeconds(30)));
        Assert.True(session.Touch(Now.AddSeconds(61)));
    }

    [Fact]
    public void Session_HasAnAbsoluteLifetimeOf12Hours()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, null, Now);

        for (int minute = 4; minute < 12 * 60; minute += 4)
        {
            session.Touch(Now.AddMinutes(minute));
        }

        Assert.Equal(PlatformSessionEndReason.AbsoluteLifetime, session.ExpiryReasonAt(Now.AddHours(12)));
    }

    [Fact]
    public void Session_StepUpWindowIsFiveMinutes()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, null, Now);

        Assert.True(session.HasRecentSecondFactor(Now.AddMinutes(5)));
        Assert.False(session.HasRecentSecondFactor(Now.AddMinutes(6)));

        session.ConfirmSecondFactor(Now.AddMinutes(6));

        Assert.True(session.HasRecentSecondFactor(Now.AddMinutes(7)));
    }

    [Fact]
    public void Session_EndsAfterFiveFailedStepUps_AndASuccessResetsTheCount()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, null, Now);

        for (int attempt = 0; attempt < 4; attempt++)
        {
            Assert.False(session.RecordFailedStepUp(Now));
        }

        session.ConfirmSecondFactor(Now.AddMinutes(1));
        Assert.Equal(0, session.FailedStepUpCount);

        for (int attempt = 0; attempt < 4; attempt++)
        {
            Assert.False(session.RecordFailedStepUp(Now.AddMinutes(2)));
        }

        Assert.True(session.RecordFailedStepUp(Now.AddMinutes(2)));
        Assert.Equal(PlatformSessionEndReason.StepUpFailed, session.EndReason);
        AssertError(PlatformIdentityErrors.SessionEnded, () => session.RecordFailedStepUp(Now.AddMinutes(3)));
    }

    [Fact]
    public void Session_EndedIsInvalid_AndCannotBeTouched()
    {
        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, null, Now);

        session.End(PlatformSessionEndReason.AccountRestricted, Now);
        session.End(PlatformSessionEndReason.SignedOut, Now.AddMinutes(1));

        Assert.False(session.IsValidAt(Now));
        Assert.Equal(PlatformSessionEndReason.AccountRestricted, session.EndReason);
        AssertError(PlatformIdentityErrors.SessionEnded, () => session.Touch(Now.AddMinutes(2)));
        Assert.False(session.HasRecentSecondFactor(Now));
    }

    [Fact]
    public void Session_RequiresAHashedToken_AndClipsTheUserAgent()
    {
        AssertError(
            PlatformIdentityErrors.TokenHashInvalid,
            () => PlatformUserSession.Start(User, "raw-token", null, null, Now));

        PlatformUserSession session = PlatformUserSession.Start(User, TokenHash, null, new string('x', 900), Now);

        Assert.Equal(PlatformIdentityLimits.UserAgentMaxLength, session.UserAgent!.Length);
    }

    [Theory]
    [InlineData(PlatformAccountTokenPurpose.Invitation, 72 * 60)]
    [InlineData(PlatformAccountTokenPurpose.PasswordReset, 60)]
    public void AccountToken_LifetimeDependsOnPurpose(PlatformAccountTokenPurpose purpose, int minutes)
    {
        PlatformAccountToken token = PlatformAccountToken.Issue(User, purpose, TokenHash, Now);

        Assert.Equal(Now.AddMinutes(minutes), token.ExpiresUtc);
        AssertError(PlatformIdentityErrors.TokenExpired, () => token.Consume(purpose, Now.AddMinutes(minutes)));
    }

    [Fact]
    public void AccountToken_IsSingleUse_AndPurposeBound()
    {
        PlatformAccountToken token = PlatformAccountToken.Issue(User, PlatformAccountTokenPurpose.PasswordReset, TokenHash, Now);

        AssertError(
            PlatformIdentityErrors.TokenPurposeInvalid,
            () => token.Consume(PlatformAccountTokenPurpose.Invitation, Now));

        token.Consume(PlatformAccountTokenPurpose.PasswordReset, Now);

        AssertError(
            PlatformIdentityErrors.TokenAlreadyUsed,
            () => token.Consume(PlatformAccountTokenPurpose.PasswordReset, Now));
    }

    [Fact]
    public void AccountToken_RevokedCannotBeUsed()
    {
        PlatformAccountToken token = PlatformAccountToken.Issue(User, PlatformAccountTokenPurpose.Invitation, TokenHash, Now);

        token.Revoke(Now);

        AssertError(
            PlatformIdentityErrors.TokenAlreadyUsed,
            () => token.Consume(PlatformAccountTokenPurpose.Invitation, Now));
    }

    [Fact]
    public void AuditEntry_ValidatesCodes_AndClipsFreeText()
    {
        PlatformAuditEntry entry = PlatformAuditEntry.Record(
            new PlatformAuditRecord
            {
                ActorUserId = User,
                ActionCode = "platform.users.locked",
                PermissionCode = "platform.users.lock",
                TargetType = "PlatformUser",
                TargetId = User,
                Reason = new string('r', 900),
                CorrelationId = "abc"
            },
            Now);

        Assert.Equal(PlatformIdentityLimits.ReasonMaxLength, entry.Reason!.Length);
        Assert.Equal(Now, entry.CreatedUtc);

        AssertError(
            PlatformIdentityErrors.AuditActionInvalid,
            () => PlatformAuditEntry.Record(new PlatformAuditRecord { ActionCode = "Users Locked", TargetType = "X" }, Now));
        AssertError(
            PlatformIdentityErrors.AuditTargetInvalid,
            () => PlatformAuditEntry.Record(new PlatformAuditRecord { ActionCode = "platform.users.locked", TargetType = " " }, Now));
    }

    private static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
