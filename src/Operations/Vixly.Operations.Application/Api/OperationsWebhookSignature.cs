using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsWebhookSignature.cs
//
// PURPOSE:
// Signs webhook bodies the industry way (Standard Webhooks; Slice 10).
//
// RESPONSIBILITIES:
// - Make a signing secret (whsec_ and 32 random bytes in base64)
// - Sign: HMAC-SHA256 over id.timestamp.body with the secret’s bytes, sent as webhook-signature: v1,<base64>
//
// IMPORTANT NOTES:
// - Receivers check the signature and that the timestamp is recent; the same id means the same event
// =========================================================
public static class OperationsWebhookSignature
{
    public const string SecretPrefix = "whsec_";

    public static string NewSecret() => SecretPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Sign(string secret, string eventId, long timestamp, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentNullException.ThrowIfNull(body);
        byte[] key = Convert.FromBase64String(secret.StartsWith(SecretPrefix, StringComparison.Ordinal) ? secret[SecretPrefix.Length..] : secret);
        byte[] mac = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(eventId + "." + timestamp.ToString(CultureInfo.InvariantCulture) + "." + body));
        return "v1," + Convert.ToBase64String(mac);
    }
}
