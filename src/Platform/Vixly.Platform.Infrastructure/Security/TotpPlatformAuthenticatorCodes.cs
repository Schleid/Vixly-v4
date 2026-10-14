using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using OtpNet;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Security;

// =========================================================
// FILE: TotpPlatformAuthenticatorCodes.cs
//
// PURPOSE:
// Authenticator-app support (TOTP, RFC 6238) for Platform
// staff.
//
// RESPONSIBILITIES:
// - Create a 160-bit secret, encrypt it with the Platform
//   Data Protection key ring, and build the otpauth URI
// - Check a 6-digit, 30-second code with one step of drift
//
// IMPORTANT NOTES:
// - Issuer is "Vixly": the console is always Vixly-branded
//   (ADR-0012). Tenant planes use the tenant's name
// - Rotating the Data Protection keys keeps old secrets
//   readable; deleting the key ring would force everyone to
//   enrol again
// =========================================================
public sealed class TotpPlatformAuthenticatorCodes : IPlatformAuthenticatorCodes
{
    private const string Issuer = "Vixly";

    private const string ProtectorPurpose = "Vixly.Platform.AuthenticatorApp.v1";

    private static readonly Action<ILogger, Exception?> LogUnreadableSecret =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "AuthenticatorSecretUnreadable"),
            "An authenticator secret could not be decrypted. Check that the Data Protection key ring and application name have not changed.");

    private readonly IDataProtector _protector;

    private readonly ILogger<TotpPlatformAuthenticatorCodes> _logger;

    public TotpPlatformAuthenticatorCodes(IDataProtectionProvider dataProtection, ILogger<TotpPlatformAuthenticatorCodes> logger)
    {
        ArgumentNullException.ThrowIfNull(dataProtection);

        _protector = dataProtection.CreateProtector(ProtectorPurpose);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public PlatformAuthenticatorEnrolment? Reopen(string encryptedSecret, string accountLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountLabel);

        string? manualKey = Unprotect(encryptedSecret);

        return manualKey is null
            ? null
            : new PlatformAuthenticatorEnrolment(encryptedSecret, manualKey, new OtpUri(OtpType.Totp, manualKey, accountLabel, Issuer).ToString());
    }

    public PlatformAuthenticatorEnrolment CreateEnrolment(string accountLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountLabel);

        byte[] secret = RandomNumberGenerator.GetBytes(20);
        string manualKey = Base32Encoding.ToString(secret);
        string uri = new OtpUri(OtpType.Totp, manualKey, accountLabel, Issuer).ToString();

        return new PlatformAuthenticatorEnrolment(_protector.Protect(manualKey), manualKey, uri);
    }

    public long? Verify(string encryptedSecret, string code, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        string digits = new(code.Where(char.IsDigit).ToArray());

        if (digits.Length != 6)
        {
            return null;
        }

        string? manualKey = Unprotect(encryptedSecret);

        if (manualKey is null)
        {
            return null;
        }

        Totp totp = new(Base32Encoding.ToBytes(manualKey));

        return totp.VerifyTotp(utcNow.UtcDateTime, digits, out long timeStep, new VerificationWindow(previous: 1, future: 1))
            ? timeStep
            : null;
    }

    private string? Unprotect(string encryptedSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(encryptedSecret);
        }
        catch (CryptographicException exception)
        {
            // Every code would fail from now on: make it visible to operators (D-6).
            LogUnreadableSecret(_logger, exception);
            return null;
        }
    }
}
