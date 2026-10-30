using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardProvider.cs
//
// PURPOSE:
// The company's own Stripe account (Slice 5F step 4; owner 2026-10-21: the company pastes its keys).
//
// RESPONSIBILITIES:
// - Keep the keys protected with the server's key, and what may be shown of them
// - Say whether it is a test or a live account
//
// IMPORTANT NOTES:
// - One per company (unique index); the secret key and the webhook secret are never shown again, only their last characters
// - Vixly never holds the company's money: Stripe pays the company's own account
// =========================================================
public sealed class OperationsCardProvider : OperationsEntity
{
    private OperationsCardProvider()
    {
        SecretKeyProtected = string.Empty;
        PublishableKey = string.Empty;
        WebhookSecretProtected = string.Empty;
        KeyHint = string.Empty;
        AccountName = string.Empty;
    }

    private OperationsCardProvider(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        SecretKeyProtected = string.Empty;
        PublishableKey = string.Empty;
        WebhookSecretProtected = string.Empty;
        KeyHint = string.Empty;
        AccountName = string.Empty;
    }

    public string SecretKeyProtected { get; private set; }

    public string PublishableKey { get; private set; }

    public string WebhookSecretProtected { get; private set; }

    // The secret key's last 4 characters, to recognise it.
    public string KeyHint { get; private set; }

    public bool Live { get; private set; }

    // The account's name as Stripe gives it.
    public string AccountName { get; private set; }

    public Guid ConnectedByUserId { get; private set; }

    public DateTimeOffset ConnectedUtc { get; private set; }

    // Stripe's key shapes: a secret (sk_) or restricted (rk_) key, a publishable key (pk_), a webhook signing secret (whsec_), test or live alike.
    public static bool KeysLookRight(string? secretKey, string? publishableKey, string? webhookSecret) =>
        secretKey is { Length: >= 20 and <= 255 } && (secretKey.StartsWith("sk_test_", StringComparison.Ordinal) || secretKey.StartsWith("sk_live_", StringComparison.Ordinal)
            || secretKey.StartsWith("rk_test_", StringComparison.Ordinal) || secretKey.StartsWith("rk_live_", StringComparison.Ordinal))
        && publishableKey is { Length: >= 20 and <= 255 } && (publishableKey.StartsWith("pk_test_", StringComparison.Ordinal) || publishableKey.StartsWith("pk_live_", StringComparison.Ordinal))
        && IsLive(secretKey) == publishableKey.StartsWith("pk_live_", StringComparison.Ordinal)
        && webhookSecret is { Length: >= 20 and <= 255 } && webhookSecret.StartsWith("whsec_", StringComparison.Ordinal)
        && !(secretKey + publishableKey + webhookSecret).Any(char.IsWhiteSpace);

    public static bool IsLive(string secretKey) => (secretKey ?? throw new ArgumentNullException(nameof(secretKey)))[3..].StartsWith("live_", StringComparison.Ordinal);

    public static OperationsCardProvider Connect(Guid tenantId, string secretKey, string secretKeyProtected, string publishableKey, string webhookSecretProtected, string? accountName,
        Guid byUserId, DateTimeOffset utcNow)
    {
        OperationsCardProvider provider = new(tenantId, utcNow);
        provider.Change(secretKey, secretKeyProtected, publishableKey, webhookSecretProtected, accountName, byUserId, utcNow);
        return provider;
    }

    public void Change(string secretKey, string secretKeyProtected, string publishableKey, string webhookSecretProtected, string? accountName, Guid byUserId, DateTimeOffset utcNow)
    {
        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(secretKeyProtected) || string.IsNullOrEmpty(webhookSecretProtected) || string.IsNullOrEmpty(publishableKey))
        {
            throw new DomainException(OperationsCardErrors.KeysInvalid);
        }

        (SecretKeyProtected, PublishableKey, WebhookSecretProtected) = (secretKeyProtected, publishableKey, webhookSecretProtected);
        KeyHint = secretKey[^4..];
        Live = IsLive(secretKey);
        AccountName = (accountName ?? string.Empty).Trim() is { Length: > 0 } name ? name[..Math.Min(name.Length, 200)] : string.Empty;
        (ConnectedByUserId, ConnectedUtc) = (byUserId, utcNow);
        MarkAsModified(utcNow);
    }
}
