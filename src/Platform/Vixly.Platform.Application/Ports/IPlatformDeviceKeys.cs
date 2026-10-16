namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformDeviceKeys.cs
//
// PURPOSE:
// Checks device public keys and signatures (key-pair proof, D-L5).
//
// RESPONSIBILITIES:
// - Say whether a public key is a valid ECDSA P-256 key
// - Verify a challenge signature
//
// IMPORTANT NOTES:
// - Keys arrive as base64 SubjectPublicKeyInfo; signatures as base64 IEEE P1363 (what WebCrypto produces)
// =========================================================
public interface IPlatformDeviceKeys
{
    bool IsValidPublicKey(string? publicKey);

    bool Verify(string publicKey, byte[] challenge, string? signature);
}
