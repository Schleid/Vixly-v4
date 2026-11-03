using System.Security.Cryptography;
using Vixly.Shared.Kernel.Security;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: VixlyCallerKeyTests.cs
//
// PURPOSE:
// Proves both written forms of a caller's private key give the
// same key, and that anything else is refused.
//
// RESPONSIBILITIES:
// - Hex (any case, wrapped) and base64 PKCS#8 sign for the same public key
// - Another curve, a damaged value and text are refused
//
// IMPORTANT NOTES:
// - The hex form is what the key tool prints since 2026-10-24
// =========================================================
public sealed class VixlyCallerKeyTests
{
    [Fact]
    public void HexAndBase64_AreTheSameKey_AndSignForItsPublicKey()
    {
        using ECDsa original = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string publicKey = Convert.ToBase64String(original.ExportSubjectPublicKeyInfo());
        string hex = VixlyCallerKey.ToHex(original);

        Assert.Equal(VixlyCallerKey.HexLength, hex.Length);

        foreach (string written in new[] { hex, hex.ToUpperInvariant(), hex[..32] + "\r\n  " + hex[32..], Convert.ToBase64String(original.ExportPkcs8PrivateKey()) })
        {
            using ECDsa key = VixlyCallerKey.Import(written);
            byte[] signature = key.SignData(new byte[] { 1, 2, 3 }, HashAlgorithmName.SHA256);

            Assert.Equal(publicKey, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
            Assert.True(original.VerifyData(new byte[] { 1, 2, 3 }, signature, HashAlgorithmName.SHA256));
        }
    }

    [Fact]
    public void OtherCurves_DamagedValues_AndText_AreRefused()
    {
        using ECDsa p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using ECDsa p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string base64 = Convert.ToBase64String(p256.ExportPkcs8PrivateKey());

        Assert.False(VixlyCallerKey.IsValid(Convert.ToBase64String(p384.ExportPkcs8PrivateKey())));
        Assert.False(VixlyCallerKey.IsValid(base64.Replace('+', ' ').Replace('/', '_')[..^4]));
        Assert.False(VixlyCallerKey.IsValid(Convert.ToBase64String(p256.ExportSubjectPublicKeyInfo())));
        Assert.False(VixlyCallerKey.IsValid("not-a-key"));
        Assert.False(VixlyCallerKey.IsValid(new string('0', VixlyCallerKey.HexLength)));
        Assert.False(VixlyCallerKey.IsValid(null));
    }
}
