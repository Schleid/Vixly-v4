using System.Security.Cryptography;
using System.Text;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeIdentityServices.cs
//
// PURPOSE:
// Stand-ins for hashing, secrets, authenticator codes, mail, links and request facts.
//
// RESPONSIBILITIES:
// - Make results predictable for tests
//
// IMPORTANT NOTES:
// - Tests only. The code 123456 is always right for the current time step
// =========================================================
internal sealed class FakeIdentityServices : IOperationsPasswordHasher, IOperationsSecretGenerator, IOperationsAuthenticatorCodes, IOperationsIdentityMailer, IOperationsPortalLinks, IOperationsRequestMetadata
{
    public const string GoodCode = "123456";

    private int _next;

    public List<OperationsOwnerInvitationMail> Mails { get; } = new();

    public List<OperationsPasswordResetMail> ResetMails { get; } = new();

    public List<string> Issuers { get; } = new();

    public bool MailFails { get; set; }

    public string? ClientIp => "203.0.113.7";

    public string? UserAgent => "test";

    public string? CorrelationId => "trace";

    public string Hash(string password) => "h:" + password;

    public OperationsPasswordCheck Verify(string password, string passwordHash) =>
        passwordHash == "h:" + password ? OperationsPasswordCheck.Succeeded : OperationsPasswordCheck.Failed;

    public OperationsSecret NewToken()
    {
        string value = "token-" + (++_next).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new OperationsSecret(value, HashToken(value));
    }

    public OperationsSecret NewRecoveryCode()
    {
        string value = "CODE" + (++_next).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        return new OperationsSecret(value, HashRecoveryCode(value));
    }

    public string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public string HashRecoveryCode(string code) => HashToken(code.Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant());

    public OperationsAuthenticatorEnrolment CreateEnrolment(string issuer, string accountLabel)
    {
        Issuers.Add(issuer);
        return new OperationsAuthenticatorEnrolment("enc-" + (++_next).ToString(System.Globalization.CultureInfo.InvariantCulture), "MANUALKEY", "otpauth://totp/" + issuer + ":" + accountLabel);
    }

    public OperationsAuthenticatorEnrolment? Reopen(string encryptedSecret, string issuer, string accountLabel) =>
        new(encryptedSecret, "MANUALKEY", "otpauth://totp/" + issuer + ":" + accountLabel);

    public long? Verify(string encryptedSecret, string code, DateTimeOffset utcNow) =>
        code == GoodCode ? utcNow.ToUnixTimeSeconds() / 30 : null;

    public Task SendPasswordResetAsync(OperationsPasswordResetMail mail, CancellationToken cancellationToken)
    {
        ResetMails.Add(mail);
        return Task.CompletedTask;
    }

    public Task SendOwnerInvitationAsync(OperationsOwnerInvitationMail mail, CancellationToken cancellationToken)
    {
        if (MailFails)
        {
            throw new InvalidOperationException("mail down");
        }

        Mails.Add(mail);
        return Task.CompletedTask;
    }

    public string Page(string slug, string pathAndQuery) => "https://" + slug + ".techcrafters.test" + pathAndQuery;
}
