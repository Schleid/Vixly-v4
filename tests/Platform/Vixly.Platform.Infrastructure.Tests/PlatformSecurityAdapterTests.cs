using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Security;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformSecurityAdapterTests.cs
//
// PURPOSE:
// Proves the security adapters: BCrypt hashing, secrets and
// hashes, TOTP codes, and that the permission query
// translates to SQL.
//
// RESPONSIBILITIES:
// - BCrypt: cost 12, wrong password fails, a lower-cost (V3)
//   hash verifies and asks for an upgrade, a malformed hash
//   fails quietly
// - Secrets: random, hashed as lowercase hex SHA-256,
//   recovery codes typed loosely still match
// - TOTP: the current code matches, a wrong or malformed
//   code does not, the secret is stored encrypted
// - The permission query has no client-side evaluation
//
// IMPORTANT NOTES:
// - Uses an ephemeral Data Protection provider; no database
// =========================================================
public sealed class PlatformSecurityAdapterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BCrypt_HashesAtCost12_AndVerifies()
    {
        BCryptPlatformPasswordHasher hasher = new();

        string hash = hasher.Hash("correct horse battery");

        Assert.StartsWith("$2a$12$", hash, StringComparison.Ordinal);
        Assert.Equal(PlatformPasswordCheck.Succeeded, hasher.Verify("correct horse battery", hash));
        Assert.Equal(PlatformPasswordCheck.Failed, hasher.Verify("wrong horse battery", hash));
    }

    [Fact]
    public void BCrypt_LowerCostHash_FromV3_VerifiesAndAsksForUpgrade()
    {
        string v3Hash = BCrypt.Net.BCrypt.HashPassword("legacy password", 10);

        Assert.Equal(
            PlatformPasswordCheck.SucceededNeedsRehash,
            new BCryptPlatformPasswordHasher().Verify("legacy password", v3Hash));
    }

    [Fact]
    public void BCrypt_MalformedHash_FailsQuietly()
    {
        Assert.Equal(PlatformPasswordCheck.Failed, new BCryptPlatformPasswordHasher().Verify("x", "not-a-hash"));
    }

    [Fact]
    public void Tokens_AreRandom_AndHashedAsHex()
    {
        PlatformSecretGenerator generator = new();

        PlatformSecret first = generator.NewToken();
        PlatformSecret second = generator.NewToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
        Assert.Equal(first.Hash, generator.HashToken(first.Value));
        Assert.DoesNotContain('=', first.Value);
    }

    [Fact]
    public void RecoveryCodes_MatchWhenTypedLoosely()
    {
        PlatformSecretGenerator generator = new();

        PlatformSecret code = generator.NewRecoveryCode();
        string typed = " " + code.Value.Replace("-", " ", StringComparison.Ordinal).ToLowerInvariant() + " ";

        Assert.Matches("^[A-Z2-9]{5}-[A-Z2-9]{5}$", code.Value);
        Assert.Equal(code.Hash, generator.HashRecoveryCode(typed));
    }

    [Fact]
    public void Totp_AcceptsTheCurrentCode_AndStoresTheSecretEncrypted()
    {
        TotpPlatformAuthenticatorCodes codes = new(new EphemeralDataProtectionProvider(), Microsoft.Extensions.Logging.Abstractions.NullLogger<TotpPlatformAuthenticatorCodes>.Instance);

        PlatformAuthenticatorEnrolment enrolment = codes.CreateEnrolment("ada@example.com");
        string current = new Totp(Base32Encoding.ToBytes(enrolment.ManualKey)).ComputeTotp(Now.UtcDateTime);

        Assert.DoesNotContain(enrolment.ManualKey, enrolment.EncryptedSecret, StringComparison.Ordinal);
        Assert.StartsWith("otpauth://totp/", enrolment.OtpAuthUri, StringComparison.Ordinal);
        Assert.Contains("issuer=Vixly", enrolment.OtpAuthUri, StringComparison.Ordinal);
        Assert.NotNull(codes.Verify(enrolment.EncryptedSecret, current, Now));
        Assert.NotNull(codes.Verify(enrolment.EncryptedSecret, current[..3] + " " + current[3..], Now));
    }

    [Fact]
    public void Totp_PendingSecret_ReopensWithTheSameKey_AndAForeignOneDoesNot()
    {
        TotpPlatformAuthenticatorCodes codes = new(new EphemeralDataProtectionProvider(), Microsoft.Extensions.Logging.Abstractions.NullLogger<TotpPlatformAuthenticatorCodes>.Instance);
        TotpPlatformAuthenticatorCodes other = new(new EphemeralDataProtectionProvider(), Microsoft.Extensions.Logging.Abstractions.NullLogger<TotpPlatformAuthenticatorCodes>.Instance);

        PlatformAuthenticatorEnrolment enrolment = codes.CreateEnrolment("ada@example.com");
        PlatformAuthenticatorEnrolment? reopened = codes.Reopen(enrolment.EncryptedSecret, "ada@example.com");

        Assert.Equal(enrolment.ManualKey, reopened!.ManualKey);
        Assert.Equal(enrolment.OtpAuthUri, reopened.OtpAuthUri);
        Assert.Null(other.Reopen(enrolment.EncryptedSecret, "ada@example.com"));
    }

    [Fact]
    public void Totp_RejectsWrongMalformedAndExpiredCodes()
    {
        TotpPlatformAuthenticatorCodes codes = new(new EphemeralDataProtectionProvider(), Microsoft.Extensions.Logging.Abstractions.NullLogger<TotpPlatformAuthenticatorCodes>.Instance);
        PlatformAuthenticatorEnrolment enrolment = codes.CreateEnrolment("ada@example.com");
        string current = new Totp(Base32Encoding.ToBytes(enrolment.ManualKey)).ComputeTotp(Now.UtcDateTime);
        string wrong = current == "000000" ? "111111" : "000000";

        Assert.Null(codes.Verify(enrolment.EncryptedSecret, wrong, Now));
        Assert.Null(codes.Verify(enrolment.EncryptedSecret, "12345", Now));
        Assert.Null(codes.Verify("not-encrypted", current, Now));
        Assert.Null(codes.Verify(enrolment.EncryptedSecret, current, Now.AddMinutes(5)));
    }

    [Fact]
    public void PermissionQuery_TranslatesToSql()
    {
        using PlatformDbContext context = new PlatformDbContextDesignTimeFactory().CreateDbContext(Array.Empty<string>());

        string sql = PlatformPermissionReader.PermissionCodesQuery(context, Guid.CreateVersion7(Now), Now).ToQueryString();

        Assert.Contains("[platform].[RoleAssignments]", sql, StringComparison.Ordinal);
        Assert.Contains("[platform].[RolePermissions]", sql, StringComparison.Ordinal);
        Assert.Contains("DISTINCT", sql, StringComparison.Ordinal);
    }
}
