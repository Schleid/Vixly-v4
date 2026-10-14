using System.Security.Cryptography;
using System.Text;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeCrypto.cs
//
// PURPOSE:
// Deterministic stand-ins for the password hasher, secret
// generator and authenticator codes.
//
// RESPONSIBILITIES:
// - Hasher: "H:" + password; "L:" marks a legacy hash that
//   verifies and asks for an upgrade; counts Verify calls
// - Secrets: sequential tokens with real SHA-256 hashes
// - Authenticator: code "123456" is valid at a moving time
//   step; anything else is wrong
// - Passkeys: an answer "good:<hex id>:<counter>" verifies;
//   "bad:<hex id>" names a credential but does not verify
//
// IMPORTANT NOTES:
// - Real crypto is tested in Infrastructure.Tests
// =========================================================
internal sealed class FakeCrypto : IPlatformPasswordHasher, IPlatformSecretGenerator, IPlatformAuthenticatorCodes, IPlatformPasskeys
{
    public const string ValidCode = "123456";

    private int _counter;

    private long _timeStep = 1000;

    public int VerifyCalls { get; private set; }

    public string Hash(string password)
    {
        return "H:" + password;
    }

    public PlatformPasswordCheck Verify(string password, string passwordHash)
    {
        VerifyCalls++;

        if (passwordHash == "L:" + password)
        {
            return PlatformPasswordCheck.SucceededNeedsRehash;
        }

        return passwordHash == "H:" + password ? PlatformPasswordCheck.Succeeded : PlatformPasswordCheck.Failed;
    }

    public PlatformSecret NewToken()
    {
        string value = "token-" + Interlocked.Increment(ref _counter);
        return new PlatformSecret(value, HashToken(value));
    }

    public PlatformSecret NewRecoveryCode()
    {
        string value = "CODE" + Interlocked.Increment(ref _counter).ToString("D6");
        return new PlatformSecret(value, HashRecoveryCode(value));
    }

    public string HashToken(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public string HashRecoveryCode(string code)
    {
        return HashToken(code.Trim().ToUpperInvariant());
    }

    public PlatformAuthenticatorEnrolment CreateEnrolment(string accountLabel)
    {
        return new PlatformAuthenticatorEnrolment("encrypted-secret", "MANUALKEY", "otpauth://totp/Vixly:" + accountLabel);
    }

    public PlatformAuthenticatorEnrolment? Reopen(string encryptedSecret, string accountLabel)
    {
        return encryptedSecret.StartsWith("encrypted-", StringComparison.Ordinal)
            ? new PlatformAuthenticatorEnrolment(encryptedSecret, "MANUALKEY", "otpauth://totp/Vixly:" + accountLabel)
            : null;
    }

    public long? Verify(string encryptedSecret, string code, DateTimeOffset utcNow)
    {
        return code == ValidCode ? ++_timeStep : null;
    }

    public string CreateRegistrationOptions(PlatformUser user, IReadOnlyCollection<byte[]> excludedCredentialIds)
    {
        return "register:" + user.Id + ":" + excludedCredentialIds.Count;
    }

    public Task<PlatformPasskeyCredential?> VerifyRegistrationAsync(string optionsJson, string attestationJson, CancellationToken cancellationToken)
    {
        string[] parts = attestationJson.Split(':');

        return Task.FromResult(parts[0] == "good" && optionsJson.StartsWith("register:", StringComparison.Ordinal)
            ? new PlatformPasskeyCredential(Convert.FromHexString(parts[1]), new byte[] { 1, 2, 3 }, 0, Guid.Empty)
            : null);
    }

    public string CreateAssertionOptions(IReadOnlyCollection<byte[]> allowedCredentialIds)
    {
        return "assert:" + allowedCredentialIds.Count;
    }

    public byte[]? ReadCredentialId(string assertionJson)
    {
        string[] parts = assertionJson.Split(':');

        return parts.Length >= 2 ? Convert.FromHexString(parts[1]) : null;
    }

    public Task<long?> VerifyAssertionAsync(string optionsJson, string assertionJson, PlatformUserPasskey passkey, CancellationToken cancellationToken)
    {
        string[] parts = assertionJson.Split(':');

        return Task.FromResult<long?>(parts.Length == 3 && parts[0] == "good" && optionsJson.StartsWith("assert:", StringComparison.Ordinal)
            ? long.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)
            : null);
    }
}
