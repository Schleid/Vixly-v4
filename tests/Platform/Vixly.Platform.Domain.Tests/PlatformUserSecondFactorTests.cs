using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformUserSecondFactorTests.cs
//
// PURPOSE:
// Proves the second-factor rules of ADR-0006 on the staff
// user: passkeys, authenticator app, recovery codes and the
// administrator reset.
//
// RESPONSIBILITIES:
// - Passkeys: validation, duplicates, limit, counter check
// - Authenticator: enrol, confirm, no code replay
// - Recovery codes: exactly ten, single use
// - A user never removes their own last second factor
// - Reset by an administrator only, never on oneself
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformUserSecondFactorTests
{
    private static readonly Guid Admin = Guid.CreateVersion7(Now);

    private static readonly byte[] CredentialA = { 1, 2, 3, 4 };

    private static readonly byte[] CredentialB = { 5, 6, 7, 8 };

    private static readonly byte[] Key = { 9, 9, 9 };

    [Fact]
    public void NewUser_HasNoSecondFactor()
    {
        Assert.False(Active().HasSecondFactor);
    }

    [Fact]
    public void RegisterPasskey_GivesASecondFactor_AndCopiesTheBytes()
    {
        PlatformUser user = Active();
        byte[] credential = { 1, 2, 3 };

        PlatformUserPasskey passkey = user.RegisterPasskey(credential, Key, 0, Guid.Empty, "Laptop", Now);
        credential[0] = 42;

        Assert.True(user.HasSecondFactor);
        Assert.Equal(1, passkey.CredentialId[0]);
    }

    [Fact]
    public void RegisterPasskey_RejectsDuplicatesAndBadInput()
    {
        PlatformUser user = Active();
        user.RegisterPasskey(CredentialA, Key, 0, Guid.Empty, "Laptop", Now);

        AssertError(
            PlatformIdentityErrors.PasskeyAlreadyRegistered,
            () => user.RegisterPasskey(CredentialA, Key, 0, Guid.Empty, "Phone", Now));
        AssertError(
            PlatformIdentityErrors.PasskeyInvalid,
            () => user.RegisterPasskey(Array.Empty<byte>(), Key, 0, Guid.Empty, "Phone", Now));
        AssertError(
            PlatformIdentityErrors.NameRequired,
            () => user.RegisterPasskey(CredentialB, Key, 0, Guid.Empty, " ", Now));
    }

    [Fact]
    public void RegisterPasskey_StopsAtTheLimit()
    {
        PlatformUser user = Active();

        for (byte index = 0; index < PlatformIdentityLimits.MaxPasskeysPerUser; index++)
        {
            user.RegisterPasskey(new[] { index }, Key, 0, Guid.Empty, "Key", Now);
        }

        AssertError(
            PlatformIdentityErrors.PasskeyLimitReached,
            () => user.RegisterPasskey(new byte[] { 200 }, Key, 0, Guid.Empty, "Key", Now));
    }

    [Fact]
    public void PasskeyCounter_MustGrow_WhenTheAuthenticatorCounts()
    {
        PlatformUser user = Active();
        user.RegisterPasskey(CredentialA, Key, 5, Guid.Empty, "YubiKey", Now);

        user.RecordPasskeySignIn(CredentialA, 6, Now);

        AssertError(
            PlatformIdentityErrors.PasskeyCounterRegressed,
            () => user.RecordPasskeySignIn(CredentialA, 6, Now));
        AssertError(PlatformIdentityErrors.PasskeyNotFound, () => user.RecordPasskeySignIn(CredentialB, 1, Now));
    }

    [Fact]
    public void PasskeyCounter_OfZero_IsAccepted_ForSyncedPasskeys()
    {
        PlatformUser user = Active();
        user.RegisterPasskey(CredentialA, Key, 0, Guid.Empty, "Phone", Now);

        user.RecordPasskeySignIn(CredentialA, 0, Now);
        user.RecordPasskeySignIn(CredentialA, 0, Now.AddMinutes(1));

        Assert.Equal(Now.AddMinutes(1), user.Passkeys.Single().LastUsedUtc);
    }

    [Fact]
    public void LastSecondFactor_CannotBeRemovedByTheUser()
    {
        PlatformUser user = Active();
        PlatformUserPasskey only = user.RegisterPasskey(CredentialA, Key, 0, Guid.Empty, "Laptop", Now);

        AssertError(PlatformIdentityErrors.LastSecondFactor, () => user.RemovePasskey(only.Id, Now));

        user.BeginAuthenticatorEnrolment("encrypted", Now);
        user.ConfirmAuthenticatorApp(100, Now);
        user.RemovePasskey(only.Id, Now);

        AssertError(PlatformIdentityErrors.LastSecondFactor, () => user.RemoveAuthenticatorApp(Now));
        Assert.True(user.HasSecondFactor);
    }

    [Fact]
    public void AuthenticatorApp_CountsOnlyOnceConfirmed_AndRefusesReplay()
    {
        PlatformUser user = Active();
        user.BeginAuthenticatorEnrolment("encrypted", Now);

        Assert.False(user.HasSecondFactor);
        AssertError(PlatformIdentityErrors.AuthenticatorNotConfirmed, () => user.RecordAuthenticatorSignIn(101, Now));

        user.ConfirmAuthenticatorApp(100, Now);
        user.RecordAuthenticatorSignIn(101, Now);

        Assert.True(user.HasSecondFactor);
        AssertError(PlatformIdentityErrors.AuthenticatorCodeReused, () => user.RecordAuthenticatorSignIn(101, Now));
        AssertError(PlatformIdentityErrors.AuthenticatorCodeReused, () => user.RecordAuthenticatorSignIn(100, Now));
        AssertError(PlatformIdentityErrors.AuthenticatorNotPending, () => user.BeginAuthenticatorEnrolment("x", Now));
    }

    [Fact]
    public void RecoveryCodes_AreTen_AndSingleUse()
    {
        PlatformUser user = Active();
        string[] hashes = Hashes(10);

        user.ReplaceRecoveryCodes(hashes, Now);
        user.UseRecoveryCode(hashes[3], Now);

        Assert.Equal(9, user.UnusedRecoveryCodeCount);
        AssertError(PlatformIdentityErrors.RecoveryCodeNotAccepted, () => user.UseRecoveryCode(hashes[3], Now));
        AssertError(PlatformIdentityErrors.RecoveryCodeNotAccepted, () => user.UseRecoveryCode(new string('f', 64), Now));
    }

    [Fact]
    public void RecoveryCodes_RejectWrongCountDuplicatesAndBadHashes()
    {
        PlatformUser user = Active();
        string[] hashes = Hashes(10);

        AssertError(PlatformIdentityErrors.RecoveryCodesInvalid, () => user.ReplaceRecoveryCodes(Hashes(9), Now));
        AssertError(
            PlatformIdentityErrors.RecoveryCodesInvalid,
            () => user.ReplaceRecoveryCodes(hashes.Take(9).Append(hashes[0]).ToArray(), Now));
        AssertError(
            PlatformIdentityErrors.TokenHashInvalid,
            () => user.ReplaceRecoveryCodes(hashes.Take(9).Append("NOT-HEX").ToArray(), Now));
    }

    [Fact]
    public void ResetSecondFactor_ClearsEverything_ButNotOnOneself()
    {
        PlatformUser user = Active();
        user.RegisterPasskey(CredentialA, Key, 0, Guid.Empty, "Laptop", Now);
        user.ReplaceRecoveryCodes(Hashes(10), Now);

        AssertError(PlatformIdentityErrors.CannotActOnOwnAccount, () => user.ResetSecondFactor("lost", user.Id, Now));
        AssertError(PlatformIdentityErrors.ReasonRequired, () => user.ResetSecondFactor(" ", Admin, Now));

        user.ResetSecondFactor("lost phone", Admin, Now);

        Assert.False(user.HasSecondFactor);
        Assert.Empty(user.RecoveryCodes);
    }

    private static string[] Hashes(int count)
    {
        return Enumerable.Range(0, count).Select(index => index.ToString("x64")).ToArray();
    }

    private static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
