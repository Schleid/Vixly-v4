using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Public.Web.Hosting;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PublicCallerSigner.cs
//
// PURPOSE:
// Signs the short service token that tells Platform.Api the visitor's IP (Stage 2, ADR-0014 scheme).
//
// RESPONSIBILITIES:
// - Load the public caller's P-256 private key once
// - Sign an ES256 token with the visitor IP, 60 seconds, used once
//
// IMPORTANT NOTES:
// - Without a key the site still works; Platform then counts limits per server address (logged once)
// - The key comes from the environment only (S-11)
// =========================================================
public sealed class PublicCallerSigner : IDisposable
{
    private readonly ECDsa? _key;

    private readonly string? _keyId;

    private readonly TimeProvider _timeProvider;

    public PublicCallerSigner(IOptions<PublicSiteOptions> options, TimeProvider timeProvider, ILogger<PublicCallerSigner> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        PublicSiteOptions site = options.Value;

        if (string.IsNullOrWhiteSpace(site.CallerPrivateKey) || string.IsNullOrWhiteSpace(site.CallerKeyId))
        {
            logger.LogWarning("No public caller key is set: Platform will count limits per server address.");
            return;
        }

        _key = ECDsa.Create();
        _key.ImportPkcs8PrivateKey(Convert.FromBase64String(site.CallerPrivateKey), out _);
        _keyId = site.CallerKeyId;
    }

    public bool Enabled => _key is not null;

    public string? Sign(string? visitorIp)
    {
        if (_key is null || string.IsNullOrWhiteSpace(visitorIp))
        {
            return null;
        }

        long now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        string header = Encode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "ES256", ["typ"] = "JWT", ["kid"] = _keyId! }));
        string payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = "public",
            ["aud"] = "vixly-platform-internal",
            ["iat"] = now,
            ["exp"] = now + 60,
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["ip"] = visitorIp
        }));

        byte[] signature;

        lock (_key)
        {
            signature = _key.SignData(Encoding.ASCII.GetBytes(header + "." + payload), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        return header + "." + payload + "." + Encode(signature);
    }

    public void Dispose() => _key?.Dispose();

    private static string Encode(byte[] data) => Base64Url.EncodeToString(data);
}
