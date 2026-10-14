using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformUserTests.cs
//
// PURPOSE:
// Proves the staff user rules: profile validation, account
// lifecycle, sign-in lockout and the administrative safety
// rules carried from V3.
//
// RESPONSIBILITIES:
// - Invitation and activation
// - Lock, suspend, deactivate, reactivate with reason and
//   actor
// - No administrative action on one's own account
// - Never remove the last active SuperUser
// - Failed sign-in lockout (5 failures, 15 minutes)
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformUserTests
{
    private static readonly Guid Admin = Guid.CreateVersion7(Now);

    [Fact]
    public void Invite_TrimsNames_AndStartsInvited()
    {
        PlatformUser user = PlatformUser.Invite(
            "  Ada ",
            " Lovelace",
            EmailAddress.Create("ada@example.com"),
            LanguageCode.Fr,
            PlatformUserAuthorityLevel.Standard,
            "  ",
            null,
            Now);

        Assert.Equal("Ada", user.FirstName);
        Assert.Equal("Lovelace", user.LastName);
        Assert.Null(user.EmployeeNumber);
        Assert.Equal(LanguageCode.Fr, user.PreferredLanguage);
        Assert.Equal(PlatformUserAccountStatus.Invited, user.Account.Status);
        Assert.Equal(user.Id, user.Account.PlatformUserId);
        Assert.False(user.Account.IsSignInAllowed(Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Invite_RejectsBlankName(string name)
    {
        AssertError(PlatformIdentityErrors.NameRequired, () => PlatformUser.Invite(
            name, "Lovelace", EmailAddress.Create("ada@example.com"), LanguageCode.En,
            PlatformUserAuthorityLevel.Standard, null, null, Now));
    }

    [Fact]
    public void Invite_RejectsUnknownAuthorityLevel()
    {
        AssertError(PlatformIdentityErrors.AuthorityLevelInvalid, () => PlatformUser.Invite(
            "Ada", "Lovelace", EmailAddress.Create("ada@example.com"), LanguageCode.En,
            (PlatformUserAuthorityLevel)99, null, null, Now));
    }

    [Fact]
    public void AcceptInvitation_ActivatesOnce()
    {
        PlatformUser user = Invited();

        user.AcceptInvitation(Hash, Now);

        Assert.True(user.Account.IsActive);
        Assert.Equal(Now, user.Account.ActivatedUtc);
        Assert.True(user.Account.IsSignInAllowed(Now));
        AssertError(PlatformIdentityErrors.InvitationNotPending, () => user.AcceptInvitation(Hash, Now));
    }

    [Fact]
    public void AcceptInvitation_RequiresAHash()
    {
        AssertError(PlatformIdentityErrors.PasswordHashRequired, () => Invited().AcceptInvitation(" ", Now));
    }

    [Fact]
    public void FiveFailedSignIns_LockSignInFor15Minutes()
    {
        PlatformUser user = Active();

        for (int attempt = 0; attempt < 4; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        Assert.True(user.Account.IsSignInAllowed(Now));

        user.RecordFailedSignIn(Now);

        Assert.False(user.Account.IsSignInAllowed(Now));
        Assert.False(user.Account.IsSignInAllowed(Now.AddMinutes(14)));
        Assert.True(user.Account.IsSignInAllowed(Now.AddMinutes(15)));
        Assert.Equal(0, user.Account.FailedSignInCount);
    }

    [Fact]
    public void SuccessfulSignIn_ResetsFailures()
    {
        PlatformUser user = Active();
        user.RecordFailedSignIn(Now);

        user.RecordSuccessfulSignIn(Now);

        Assert.Equal(0, user.Account.FailedSignInCount);
        Assert.Equal(Now, user.Account.LastSignInUtc);
    }

    [Fact]
    public void SuccessfulSignIn_IsRefusedDuringLockout()
    {
        PlatformUser user = Active();

        for (int attempt = 0; attempt < PlatformIdentityLimits.MaxFailedSignIns; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        AssertError(PlatformIdentityErrors.AccountNotActive, () => user.RecordSuccessfulSignIn(Now));
    }

    [Fact]
    public void Lock_RecordsReasonAndActor_AndBlocksSignIn()
    {
        PlatformUser user = Active();

        user.Lock(" left the company ", Admin, 1, Now);

        Assert.Equal(PlatformUserAccountStatus.Locked, user.Account.Status);
        Assert.Equal("left the company", user.Account.StatusReason);
        Assert.Equal(Admin, user.Account.StatusChangedByUserId);
        Assert.False(user.Account.IsSignInAllowed(Now));
    }

    [Fact]
    public void Restrictions_RequireAReason()
    {
        PlatformUser user = Active();

        AssertError(PlatformIdentityErrors.ReasonRequired, () => user.Suspend(" ", Admin, 1, Now));
        AssertError(PlatformIdentityErrors.ReasonRequired, () => user.Deactivate("", Admin, 1, Now));
    }

    [Fact]
    public void AdministrativeActions_OnOwnAccount_AreRefused()
    {
        PlatformUser user = Active(PlatformUserAuthorityLevel.Administrator);

        AssertError(PlatformIdentityErrors.CannotActOnOwnAccount, () => user.Lock("x", user.Id, 1, Now));
        AssertError(PlatformIdentityErrors.CannotActOnOwnAccount, () => user.Suspend("x", user.Id, 1, Now));
        AssertError(PlatformIdentityErrors.CannotActOnOwnAccount, () => user.Deactivate("x", user.Id, 1, Now));
        AssertError(
            PlatformIdentityErrors.CannotActOnOwnAccount,
            () => user.ChangeAuthorityLevel(PlatformUserAuthorityLevel.SuperUser, user.Id, 1, Now));
        AssertError(
            PlatformIdentityErrors.CannotActOnOwnAccount,
            () => user.AssignRole(Guid.CreateVersion7(Now), user.Id, null, Now));
    }

    [Fact]
    public void LastActiveSuperUser_CannotBeRemoved()
    {
        PlatformUser superUser = Active(PlatformUserAuthorityLevel.SuperUser);

        AssertError(PlatformIdentityErrors.LastSuperUser, () => superUser.Lock("x", Admin, 0, Now));
        AssertError(PlatformIdentityErrors.LastSuperUser, () => superUser.Suspend("x", Admin, 0, Now));
        AssertError(PlatformIdentityErrors.LastSuperUser, () => superUser.Deactivate("x", Admin, 0, Now));
        AssertError(
            PlatformIdentityErrors.LastSuperUser,
            () => superUser.ChangeAuthorityLevel(PlatformUserAuthorityLevel.Administrator, Admin, 0, Now));

        Assert.True(superUser.IsActiveSuperUser);
    }

    [Fact]
    public void SuperUser_CanBeRemoved_WhenAnotherRemains()
    {
        PlatformUser superUser = Active(PlatformUserAuthorityLevel.SuperUser);

        superUser.ChangeAuthorityLevel(PlatformUserAuthorityLevel.Administrator, Admin, 1, Now);

        Assert.False(superUser.IsActiveSuperUser);
    }

    [Fact]
    public void NegativeSuperUserCount_IsRejected()
    {
        PlatformUser superUser = Active(PlatformUserAuthorityLevel.SuperUser);

        AssertError(PlatformIdentityErrors.SuperUserCountInvalid, () => superUser.Lock("x", Admin, -1, Now));
    }

    [Fact]
    public void Reactivate_ReturnsToActive_OrInvitedIfNeverActivated()
    {
        PlatformUser active = Active();
        active.Suspend("review", Admin, 1, Now);
        active.Reactivate("review done", Admin, Now);

        PlatformUser invited = Invited();
        invited.Lock("wrong person", Admin, 1, Now);
        invited.Reactivate("corrected", Admin, Now);

        Assert.Equal(PlatformUserAccountStatus.Active, active.Account.Status);
        Assert.Equal(PlatformUserAccountStatus.Invited, invited.Account.Status);
    }

    [Fact]
    public void Reactivate_RequiresARestrictedAccount()
    {
        AssertError(PlatformIdentityErrors.AccountNotRestricted, () => Active().Reactivate("x", Admin, Now));
    }

    [Fact]
    public void Deactivated_CannotBeLockedOrSuspended_ButCanBeReactivated()
    {
        PlatformUser user = Active();
        user.Deactivate("left", Admin, 1, Now);

        AssertError(PlatformIdentityErrors.AccountDeactivated, () => user.Lock("x", Admin, 1, Now));
        AssertError(PlatformIdentityErrors.AccountDeactivated, () => user.Deactivate("x", Admin, 1, Now));

        user.Reactivate("rehired", Admin, Now);

        Assert.True(user.Account.IsActive);
    }

    [Fact]
    public void ChangePassword_RequiresActiveAccount_AndClearsLockout()
    {
        PlatformUser invited = Invited();
        AssertError(PlatformIdentityErrors.AccountNotActive, () => invited.ChangePasswordHash(Hash, Now));

        PlatformUser user = Active();

        for (int attempt = 0; attempt < PlatformIdentityLimits.MaxFailedSignIns; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        user.ChangePasswordHash(Hash, Now);

        Assert.True(user.Account.IsSignInAllowed(Now));
    }

    private static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
