using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using OtpNet;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Security;

// =========================================================
// FILE: TotpOperationsAuthenticatorCodes.cs
//
// PURPOSE:
// Authenticator app (TOTP) secrets for staff, protected with the Operations key ring.
//
// RESPONSIBILITIES:
// - Create and reopen an enrolment
// - Check a 6-digit code within one step either side
//
// IMPORTANT NOTES:
// - The issuer is the tenant name (ADR-0012 W-1)
// - An unreadable secret is logged: every code would fail (D-6)
// =========================================================
public sealed class TotpOperationsAuthenticatorCodes : IOperationsAuthenticatorCodes
{
    // The demo data of Development protects its known key with the same purpose.
    public const string ProtectorPurpose = "Operations.AuthenticatorApp.v1";

    private static readonly Action<ILogger, Exception?> LogUnreadableSecret = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, "AuthenticatorSecretUnreadable"),
        "An authenticator secret could not be decrypted. Check that the Data Protection key ring and application name have not changed.");

    private readonly IDataProtector _protector;

    private readonly ILogger<TotpOperationsAuthenticatorCodes> _logger;

    public TotpOperationsAuthenticatorCodes(IDataProtectionProvider dataProtection, ILogger<TotpOperationsAuthenticatorCodes> logger)
    {
        ArgumentNullException.ThrowIfNull(dataProtection);
        _protector = dataProtection.CreateProtector(ProtectorPurpose);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public OperationsAuthenticatorEnrolment CreateEnrolment(string issuer, string accountLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountLabel);

        string manualKey = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        return new OperationsAuthenticatorEnrolment(_protector.Protect(manualKey), manualKey, new OtpUri(OtpType.Totp, manualKey, accountLabel, issuer).ToString());
    }

    public OperationsAuthenticatorEnrolment? Reopen(string encryptedSecret, string issuer, string accountLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountLabel);

        string? manualKey = Unprotect(encryptedSecret);
        return manualKey is null
            ? null
            : new OperationsAuthenticatorEnrolment(encryptedSecret, manualKey, new OtpUri(OtpType.Totp, manualKey, accountLabel, issuer).ToString());
    }

    public long? Verify(string encryptedSecret, string code, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        string digits = new(code.Where(char.IsDigit).ToArray());
        string? manualKey = digits.Length == 6 ? Unprotect(encryptedSecret) : null;

        if (manualKey is null)
        {
            return null;
        }

        Totp totp = new(Base32Encoding.ToBytes(manualKey));
        return totp.VerifyTotp(utcNow.UtcDateTime, digits, out long timeStep, new VerificationWindow(previous: 1, future: 1)) ? timeStep : null;
    }

    private string? Unprotect(string encryptedSecret)
    {
        try
        {
            return _protector.Unprotect(encryptedSecret);
        }
        catch (CryptographicException exception)
        {
            LogUnreadableSecret(_logger, exception);
            return null;
        }
    }
}
