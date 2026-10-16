using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Vixly.Platform.Api.Hosting;

namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformServiceTokenValidator.cs
//
// PURPOSE:
// Checks a caller's ES256 service token: key, signature, audience, time and one use (Slice 9, D-K1).
//
// RESPONSIBILITIES:
// - Load the callers' public keys once
// - Accept a token only when every check passes
//
// IMPORTANT NOTES:
// - Lifetime at most 2 minutes, 30 seconds clock skew
// - A token id is accepted once (replay cache per process; shared hosting runs one)
// - No package: System.Security.Cryptography only
// =========================================================
public sealed class PlatformServiceTokenValidator
{
    public const int MaxTokenLength = 8192;

    public static readonly TimeSpan MaxLifetime = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);

    private readonly Dictionary<string, string[]> _scopes = new(StringComparer.Ordinal);

    private readonly IMemoryCache _seen;

    private readonly TimeProvider _timeProvider;

    private readonly object _gate = new();

    public PlatformServiceTokenValidator(IOptions<PlatformInternalOptions> options, IMemoryCache seen, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _seen = seen ?? throw new ArgumentNullException(nameof(seen));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        foreach ((string caller, PlatformInternalCallerOptions settings) in options.Value.Callers)
        {
            string name = caller.ToLowerInvariant();
            _scopes[name] = settings.Scopes.Select(scope => scope.Trim().ToLowerInvariant()).ToArray();

            foreach ((string keyId, string publicKey) in settings.Keys)
            {
                byte[] material = Convert.FromBase64String(publicKey);

                // A key that does not load stops the start, rather than refusing every call later.
                using ECDsa check = ECDsa.Create();
                check.ImportSubjectPublicKeyInfo(material, out _);

                if (check.KeySize != 256)
                {
                    throw new InvalidOperationException($"PlatformInternal key {caller}/{keyId} must be a P-256 key.");
                }

                _keys[Key(name, keyId)] = material;
            }
        }
    }

    public PlatformServiceToken? Validate(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > MaxTokenLength)
        {
            return null;
        }

        string[] parts = token.Split('.');

        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            using JsonDocument header = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[0]));
            using JsonDocument payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
            JsonElement claims = payload.RootElement;

            if (Text(header.RootElement, "alg") != "ES256" || Text(header.RootElement, "kid") is not { } keyId
                || Text(claims, "iss")?.ToLowerInvariant() is not { } caller
                || !_scopes.TryGetValue(caller, out string[]? scopes)
                || !_keys.TryGetValue(Key(caller, keyId), out byte[]? material))
            {
                return null;
            }

            byte[] signature = Base64Url.DecodeFromChars(parts[2]);
            using ECDsa key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(material, out _);

            if (signature.Length != 64 || !key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            {
                return null;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();

            if (!Audience(claims) || Seconds(claims, "iat") is not { } issued || Seconds(claims, "exp") is not { } expires
                || expires <= issued || expires - issued > MaxLifetime || issued > now + ClockSkew || expires < now - ClockSkew
                || Text(claims, "jti") is not { Length: >= 16 and <= 100 } tokenId)
            {
                return null;
            }

            lock (_gate)
            {
                string seen = "platform.internal.jti:" + caller + ":" + tokenId;

                if (_seen.TryGetValue(seen, out _))
                {
                    return null;
                }

                _seen.Set(seen, true, expires + ClockSkew);
            }

            Guid? tenantId = Text(claims, "tid") is { } tid ? (Guid.TryParse(tid, out Guid parsed) && parsed != Guid.Empty ? parsed : throw new FormatException()) : null;

            return new PlatformServiceToken(
                caller,
                scopes,
                tenantId,
                Clip(Text(claims, "sub"), 100),
                Clip(Text(claims, "name"), 200),
                Clip(Text(claims, "role"), 20),
                claims.TryGetProperty("stepup", out JsonElement stepUp) && stepUp.ValueKind == JsonValueKind.True,
                Clip(Text(claims, "ip"), 45),
                Clip(Text(claims, "ua"), 512));
        }
        catch (Exception exception) when (exception is FormatException or JsonException or CryptographicException or ArgumentException)
        {
            return null;
        }
    }

    private static string Key(string caller, string keyId) => caller + "/" + keyId;

    private static bool Audience(JsonElement claims)
    {
        if (!claims.TryGetProperty("aud", out JsonElement audience))
        {
            return false;
        }

        return audience.ValueKind switch
        {
            JsonValueKind.String => audience.GetString() == PlatformInternalOptions.Audience,
            JsonValueKind.Array => audience.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String && item.GetString() == PlatformInternalOptions.Audience),
            _ => false
        };
    }

    private static string? Text(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTimeOffset? Seconds(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    private static string? Clip(string? value, int max)
    {
        string? text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length <= max ? text : text[..max];
    }
}
