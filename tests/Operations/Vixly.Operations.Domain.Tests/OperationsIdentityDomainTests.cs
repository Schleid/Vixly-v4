using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsIdentityDomainTests.cs
//
// PURPOSE:
// Proves the staff identity rules of Slice 1A.
//
// RESPONSIBILITIES:
// - Owner invitation, password, lockout, second factor and recovery codes
// - Sessions end after 15 idle minutes and 12 hours
// - Links and pending sign-ins are single-use and short-lived
// - Audit codes and brand names are checked
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsIdentityDomainTests
{
    // Owner, 2026-10-21; review: once a device has a country, any other code is a change, unknown (XX) and Tor (T1) included; no header and no country of its own tell nothing.
    [Theory]
    [InlineData("US", "US", false)]
    [InlineData("US", "FR", true)]
    [InlineData("US", "T1", true)]
    [InlineData("US", "XX", true)]
    [InlineData("US", null, false)]
    [InlineData(null, "FR", false)]
    public void SignInPlace_ChangesCountry_OnlyAgainstTheDevicesOwn(string? registered, string? now, bool changed)
    {
        Assert.Equal(changed, new OperationsSignInPlace(Guid.NewGuid(), "Desk", "D-000001", registered, now).CountryChanged);
        Assert.False(OperationsSignInPlace.Nowhere(now).CountryChanged);
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly string Hash = new('a', 64);

    [Fact]
    public void Owner_IsInvited_AndAlwaysNeedsASecondFactor()
    {
        OperationsUser owner = Owner();

        Assert.True(owner.IsOwner);
        Assert.True(owner.RequiresSecondFactor);
        Assert.False(owner.HasSecondFactor);
        Assert.Equal(OperationsUserAccountStatus.Invited, owner.Account.Status);
        Assert.Equal(Tenant, owner.Account.TenantId);
        Assert.False(owner.Account.IsSignInAllowed(Now));
    }

    [Fact]
    public void Owner_NeedsAName()
    {
        Assert.Equal("ops.identity.name_required", Assert.Throws<DomainException>(() =>
            OperationsUser.InviteOwner(Tenant, " ", EmailAddress.Create("owner@northwind.example"), LanguageCode.En, Now)).Error.Code);
    }

    [Fact]
    public void Invitation_ActivatesOnce()
    {
        OperationsUser owner = Owner();

        owner.AcceptInvitation("hash", Now);

        Assert.True(owner.Account.IsSignInAllowed(Now));
        Assert.Equal("ops.identity.invitation_not_pending", Assert.Throws<DomainException>(() => owner.AcceptInvitation("hash", Now)).Error.Code);
    }

    [Fact]
    public void FiveWrongPasswords_LockForFifteenMinutes()
    {
        OperationsUser owner = Active();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            owner.RecordFailedSignIn(Now);
        }

        Assert.False(owner.Account.IsSignInAllowed(Now.AddMinutes(14)));
        Assert.True(owner.Account.IsSignInAllowed(Now.AddMinutes(15)));
    }

    [Fact]
    public void AuthenticatorCode_IsAcceptedOncePerTimeStep()
    {
        OperationsUser owner = Active();
        owner.BeginAuthenticatorEnrolment("secret", Now);
        owner.ConfirmAuthenticatorApp(100, Now);

        owner.RecordAuthenticatorSignIn(101, Now);

        Assert.True(owner.HasSecondFactor);
        Assert.Equal("ops.identity.authenticator_code_reused", Assert.Throws<DomainException>(() => owner.RecordAuthenticatorSignIn(101, Now)).Error.Code);
        Assert.Equal("ops.identity.authenticator_not_pending", Assert.Throws<DomainException>(() => owner.BeginAuthenticatorEnrolment("other", Now)).Error.Code);
    }

    [Fact]
    public void RecoveryCodes_AreTen_AndEachWorksOnce()
    {
        OperationsUser owner = Active();
        string[] hashes = Enumerable.Range(0, 10).Select(index => index.ToString("x64", System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        Assert.Equal("ops.identity.recovery_codes_invalid", Assert.Throws<DomainException>(() => owner.ReplaceRecoveryCodes(hashes[..9], Now)).Error.Code);

        owner.ReplaceRecoveryCodes(hashes, Now);
        owner.UseRecoveryCode(hashes[3], Now);

        Assert.Equal(9, owner.UnusedRecoveryCodeCount);
        Assert.All(owner.RecoveryCodes, code => Assert.Equal(Tenant, code.TenantId));
        Assert.Equal("ops.identity.recovery_code_not_accepted", Assert.Throws<DomainException>(() => owner.UseRecoveryCode(hashes[3], Now)).Error.Code);
    }

    [Fact]
    public void Session_EndsAfterFifteenIdleMinutes()
    {
        OperationsUserSession session = OperationsUserSession.Start(Tenant, Guid.NewGuid(), Hash, "203.0.113.7", "browser", false, Now);

        Assert.True(session.IsValidAt(Now.AddMinutes(14)));
        Assert.Equal(OperationsSessionEndReason.IdleTimeout, session.ExpiryReasonAt(Now.AddMinutes(15)));
    }

    [Fact]
    public void Session_StaysAliveWhileUsed_ButNotPastTwelveHours()
    {
        OperationsUserSession session = OperationsUserSession.Start(Tenant, Guid.NewGuid(), Hash, null, null, false, Now);

        for (int minute = 10; minute < 12 * 60; minute += 10)
        {
            Assert.True(session.IsValidAt(Now.AddMinutes(minute)));
            session.Touch(Now.AddMinutes(minute));
        }

        Assert.Equal(OperationsSessionEndReason.AbsoluteLifetime, session.ExpiryReasonAt(Now.AddHours(12)));
    }

    [Fact]
    public void Session_TouchIsWrittenAtMostOnceAMinute_AndEndsOnce()
    {
        OperationsUserSession session = OperationsUserSession.Start(Tenant, Guid.NewGuid(), Hash, null, null, true, Now);

        Assert.False(session.Touch(Now.AddSeconds(30)));
        Assert.True(session.Touch(Now.AddMinutes(2)));
        Assert.True(session.IsSetupSession);

        session.End(OperationsSessionEndReason.SignedOut, Now.AddMinutes(3));
        session.End(OperationsSessionEndReason.IdleTimeout, Now.AddMinutes(4));

        Assert.Equal(OperationsSessionEndReason.SignedOut, session.EndReason);
        Assert.Equal("ops.identity.session_ended", Assert.Throws<DomainException>(() => session.Touch(Now.AddMinutes(5))).Error.Code);
    }

    [Fact]
    public void SignInChallenge_AllowsFiveMinutesAndFiveMistakes()
    {
        OperationsSignInChallenge challenge = OperationsSignInChallenge.Issue(Tenant, Guid.NewGuid(), Hash, OperationsSignInStage.SecondFactor, Now);

        Assert.False(challenge.IsUsableAt(Now.AddMinutes(5)));

        for (int attempt = 0; attempt < 5; attempt++)
        {
            challenge.RecordFailedAttempt(Now);
        }

        Assert.False(challenge.IsUsableAt(Now));
        Assert.Equal("ops.identity.sign_in_challenge_invalid", Assert.Throws<DomainException>(() => challenge.Complete(Now)).Error.Code);
    }

    [Fact]
    public void InvitationLink_WorksOnce_ForSeventyTwoHours()
    {
        OperationsAccountToken expired = OperationsAccountToken.Issue(Tenant, Guid.NewGuid(), OperationsAccountTokenPurpose.Invitation, Hash, Now);
        OperationsAccountToken token = OperationsAccountToken.Issue(Tenant, Guid.NewGuid(), OperationsAccountTokenPurpose.Invitation, Hash, Now);

        Assert.Equal("ops.identity.token_expired", Assert.Throws<DomainException>(() => expired.Consume(OperationsAccountTokenPurpose.Invitation, Now.AddHours(72))).Error.Code);
        Assert.Equal("ops.identity.token_purpose_invalid", Assert.Throws<DomainException>(() => token.Consume(OperationsAccountTokenPurpose.PasswordReset, Now)).Error.Code);

        token.Consume(OperationsAccountTokenPurpose.Invitation, Now.AddHours(71));

        Assert.Equal("ops.identity.token_already_used", Assert.Throws<DomainException>(() => token.Consume(OperationsAccountTokenPurpose.Invitation, Now)).Error.Code);
    }

    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void TokenHash_MustBeLowercaseSha256(string hash)
    {
        Assert.Equal("ops.identity.token_hash_invalid", Assert.Throws<DomainException>(() =>
            OperationsUserSession.Start(Tenant, Guid.NewGuid(), hash, null, null, false, Now)).Error.Code);
    }

    [Theory]
    [InlineData("platform.auth.signed_in")]
    [InlineData("ops")]
    [InlineData("ops.Auth.x")]
    public void AuditEntry_RefusesForeignCodes(string code)
    {
        Assert.Equal("ops.identity.audit_action_invalid", Assert.Throws<DomainException>(() =>
            OperationsAuditEntry.Record(Tenant, new OperationsAuditRecord { ActionCode = code, TargetType = "OperationsUser" }, Now)).Error.Code);
    }

    [Fact]
    public void AuditEntry_ClipsFreeText()
    {
        OperationsAuditEntry entry = OperationsAuditEntry.Record(
            Tenant,
            new OperationsAuditRecord { ActionCode = "ops.auth.signed_in", TargetType = "OperationsUser", UserAgent = new string('u', 600) },
            Now);

        Assert.Equal(512, entry.UserAgent!.Length);
        Assert.Equal(Tenant, entry.TenantId);
    }

    [Fact]
    public void Brand_NeedsAName_OfAtMost150Characters()
    {
        Assert.Equal("ops.brand.display_name_required", Assert.Throws<DomainException>(() => OperationsTenantBrand.Create(Tenant, " ", Now)).Error.Code);
        Assert.Equal("ops.brand.display_name_too_long", Assert.Throws<DomainException>(() => OperationsTenantBrand.Create(Tenant, new string('n', 151), Now)).Error.Code);
        Assert.Equal("Northwind Freight", OperationsTenantBrand.Create(Tenant, " Northwind Freight ", Now).DisplayName);
    }

    private static OperationsUser Owner()
    {
        return OperationsUser.InviteOwner(Tenant, "Grace Hopper", EmailAddress.Create("grace@northwind.example"), LanguageCode.En, Now);
    }

    private static OperationsUser Active()
    {
        OperationsUser owner = Owner();
        owner.AcceptInvitation("hash", Now);
        return owner;
    }
}
