using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsServiceTokenSigner.cs
//
// PURPOSE:
// Signs the service tokens Operations shows Platform.Api (ADR-0014).
//
// RESPONSIBILITIES:
// - Sign a 60-second, single-use ES256 token as the caller "operations"
// - Add the tenant claim when a call is about one tenant
//
// IMPORTANT NOTES:
// - The key is P-256 in PKCS#8, loaded once
// - Own code: Platform and Operations share no security library (ADR-0020 rejected)
// =========================================================
public sealed class OperationsServiceTokenSigner : IDisposable
{
    public const string Caller = "operations";

    public const string Audience = "vixly-platform-internal";

    private readonly ECDsa? _key;

    private readonly string _keyId;

    private readonly TimeProvider _timeProvider;

    public OperationsServiceTokenSigner(IOptions<OperationsPlatformOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _keyId = options.Value.CallerKeyId;

        if (!string.IsNullOrWhiteSpace(options.Value.CallerPrivateKey) && !string.IsNullOrWhiteSpace(_keyId))
        {
            _key = ECDsa.Create();
            _key.ImportPkcs8PrivateKey(Convert.FromBase64String(options.Value.CallerPrivateKey), out _);

            if (_key.KeySize != 256)
            {
                throw new InvalidOperationException("OperationsPlatform:CallerPrivateKey must be a P-256 key.");
            }
        }
    }

    public bool Enabled => _key is not null;

    public string Sign(Guid? tenantId)
    {
        return Sign(tenantId, null);
    }

    // With an actor, Platform sees who acts, their role and step-up (Slice 7, D-K8; ADR-0003).
    public string Sign(Guid? tenantId, OperationsPlatformActor? actor)
    {
        if (_key is null)
        {
            throw new InvalidOperationException("OperationsPlatform:CallerKeyId and CallerPrivateKey are not configured.");
        }

        long now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        string header = Encode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "ES256", ["typ"] = "JWT", ["kid"] = _keyId }));
        Dictionary<string, object> claims = new(StringComparer.Ordinal)
        {
            ["iss"] = Caller,
            ["aud"] = Audience,
            ["iat"] = now,
            ["exp"] = now + 60,
            ["jti"] = Guid.NewGuid().ToString("N")
        };

        if (tenantId is { } tenant)
        {
            claims["tid"] = tenant.ToString("D");
        }

        if (actor is not null)
        {
            claims["sub"] = actor.UserId.ToString("D");
            claims["name"] = actor.Name;
            claims["role"] = actor.Role;
            claims["stepup"] = actor.StepUp;

            if (actor.ClientIp is { Length: > 0 } ip)
            {
                claims["ip"] = ip;
            }

            if (actor.UserAgent is { Length: > 0 } agent)
            {
                claims["ua"] = agent.Length > 512 ? agent[..512] : agent;
            }
        }

        string payload = Encode(JsonSerializer.SerializeToUtf8Bytes(claims));
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
