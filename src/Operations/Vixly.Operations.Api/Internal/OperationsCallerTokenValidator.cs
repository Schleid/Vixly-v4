using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Vixly.Contracts.Operations.Internal;

namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsCallerTokenValidator.cs
//
// PURPOSE:
// Checks the ES256 tokens callers show Operations.Api (Stage 3, Slice 0, D-F4; ADR-0014 rules).
//
// RESPONSIBILITIES:
// - Check signature, audience, lifetime (2 minutes at most), clock skew and single use
// - Read the tenant and customer claims
//
// IMPORTANT NOTES:
// - Any fault reads as "no token": the caller learns nothing
// - Own code: Platform and Operations share no security library (ADR-0020 rejected)
// - Replay cache is per process (fine for one server; shared cache if Operations.Api ever runs on several)
// =========================================================
public sealed class OperationsCallerTokenValidator
{
    public const int MaxTokenLength = 8192;

    public static readonly TimeSpan MaxLifetime = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);

    private readonly Dictionary<string, string[]> _scopes = new(StringComparer.Ordinal);

    private readonly IMemoryCache _seen;

    private readonly TimeProvider _timeProvider;

    private readonly object _gate = new();

    public OperationsCallerTokenValidator(IOptions<OperationsInternalOptions> options, IMemoryCache seen, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _seen = seen ?? throw new ArgumentNullException(nameof(seen));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        foreach ((string caller, OperationsCallerOptions settings) in options.Value.Callers)
        {
            string name = caller.ToLowerInvariant();
            _scopes[name] = settings.Scopes.Select(scope => scope.Trim().ToLowerInvariant()).ToArray();

            foreach ((string keyId, string publicKey) in settings.Keys)
            {
                byte[] material = Convert.FromBase64String(publicKey);
                using ECDsa check = ECDsa.Create();
                check.ImportSubjectPublicKeyInfo(material, out _);

                if (check.KeySize != 256)
                {
                    throw new InvalidOperationException($"OperationsInternal key {caller}/{keyId} must be a P-256 key.");
                }

                _keys[caller.ToLowerInvariant() + "/" + keyId] = material;
            }
        }
    }

    public OperationsCallerToken? Validate(string? token)
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
                || !_keys.TryGetValue(caller + "/" + keyId, out byte[]? material))
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

            if (Text(claims, "aud") != OperationsCallerClaims.Audience || Seconds(claims, "iat") is not { } issued || Seconds(claims, "exp") is not { } expires
                || expires <= issued || expires - issued > MaxLifetime || issued > now + ClockSkew || expires < now - ClockSkew
                || Text(claims, "jti") is not { Length: >= 16 and <= 100 } tokenId)
            {
                return null;
            }

            lock (_gate)
            {
                string seen = "ops.internal.jti:" + caller + ":" + tokenId;

                if (_seen.TryGetValue(seen, out _))
                {
                    return null;
                }

                _seen.Set(seen, true, expires + ClockSkew);
            }

            return new OperationsCallerToken(caller, scopes, Id(claims, OperationsCallerClaims.TenantId), Id(claims, OperationsCallerClaims.CustomerId));
        }
        catch (Exception exception) when (exception is FormatException or JsonException or CryptographicException or ArgumentException)
        {
            return null;
        }
    }

    private static Guid? Id(JsonElement claims, string name)
    {
        return Text(claims, name) is { } value
            ? (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty ? parsed : throw new FormatException())
            : null;
    }

    private static string? Text(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTimeOffset? Seconds(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
}
