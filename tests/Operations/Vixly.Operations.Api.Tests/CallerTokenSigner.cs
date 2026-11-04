using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: CallerTokenSigner.cs
//
// PURPOSE:
// Signs caller tokens with any claims, so tests can break each rule.
//
// RESPONSIBILITIES:
// - Hold a P-256 key per caller
// - Sign a token
//
// IMPORTANT NOTES:
// - The private key never leaves the test
// =========================================================
internal sealed class CallerTokenSigner : IDisposable
{
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public CallerTokenSigner(string caller, string keyId = "k1")
    {
        Caller = caller;
        KeyId = keyId;
    }

    public string Caller { get; }

    public string KeyId { get; }

    public string PublicKey => Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());

    // For a real Customer.Web in the end-to-end test (Slice 2).
    public string PrivateKey => Convert.ToBase64String(_key.ExportPkcs8PrivateKey());

    public string Sign(Dictionary<string, object?>? extra = null, TimeSpan? lifetime = null, string audience = "vixly-operations-internal", DateTimeOffset? issued = null, string? jti = null)
    {
        DateTimeOffset at = issued ?? DateTimeOffset.UtcNow;
        Dictionary<string, object?> claims = new(StringComparer.Ordinal)
        {
            ["iss"] = Caller,
            ["aud"] = audience,
            ["iat"] = at.ToUnixTimeSeconds(),
            ["exp"] = at.Add(lifetime ?? TimeSpan.FromSeconds(60)).ToUnixTimeSeconds(),
            ["jti"] = jti ?? Guid.NewGuid().ToString("N")
        };

        foreach ((string name, object? value) in extra ?? new Dictionary<string, object?>())
        {
            claims[name] = value;
        }

        string header = Encode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "ES256", ["typ"] = "JWT", ["kid"] = KeyId }));
        string payload = Encode(JsonSerializer.SerializeToUtf8Bytes(claims));
        byte[] signature = _key.SignData(Encoding.ASCII.GetBytes(header + "." + payload), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return header + "." + payload + "." + Encode(signature);
    }

    public void Dispose() => _key.Dispose();

    private static string Encode(byte[] data) => Base64Url.EncodeToString(data);
}
