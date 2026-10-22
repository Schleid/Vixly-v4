using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vixly.Contracts.Operations.Internal;

namespace Vixly.Customer.Web.Operations;

// =========================================================
// FILE: CustomerCallerSigner.cs
//
// PURPOSE:
// Signs the tokens Customer.Web shows Operations.Api (Stage 3, Slice 0, D-F4).
//
// RESPONSIBILITIES:
// - Sign a 60-second, single-use ES256 token as the caller "customer"
// - Add the tenant and, once signed in, the customer
//
// IMPORTANT NOTES:
// - The key is P-256 in PKCS#8, loaded once
// - Own code (ADR-0020 rejected)
// =========================================================
public sealed class CustomerCallerSigner : IDisposable
{
    private readonly ECDsa? _key;

    private readonly string _keyId;

    private readonly TimeProvider _timeProvider;

    public CustomerCallerSigner(IOptions<CustomerOperationsOptions> options, TimeProvider timeProvider)
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
                throw new InvalidOperationException("CustomerOperations:CallerPrivateKey must be a P-256 key.");
            }
        }
    }

    public string Sign(Guid? tenantId, Guid? customerId)
    {
        if (_key is null)
        {
            throw new InvalidOperationException("CustomerOperations:CallerKeyId and CallerPrivateKey are not configured.");
        }

        long now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        string header = Encode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "ES256", ["typ"] = "JWT", ["kid"] = _keyId }));
        Dictionary<string, object> claims = new(StringComparer.Ordinal)
        {
            ["iss"] = OperationsCallerClaims.CustomerCaller,
            ["aud"] = OperationsCallerClaims.Audience,
            ["iat"] = now,
            ["exp"] = now + 60,
            ["jti"] = Guid.NewGuid().ToString("N")
        };

        if (tenantId is { } tenant)
        {
            claims[OperationsCallerClaims.TenantId] = tenant.ToString("D");
        }

        if (customerId is { } customer)
        {
            claims[OperationsCallerClaims.CustomerId] = customer.ToString("D");
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
