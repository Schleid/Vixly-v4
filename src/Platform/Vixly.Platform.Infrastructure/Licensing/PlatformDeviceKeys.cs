using System.Security.Cryptography;
using Vixly.Platform.Application.Ports;

namespace Vixly.Platform.Infrastructure.Licensing;

// =========================================================
// FILE: PlatformDeviceKeys.cs
//
// PURPOSE:
// Checks ECDSA P-256 device keys and challenge signatures (key-pair proof, D-L5).
//
// RESPONSIBILITIES:
// - Accept only P-256 SubjectPublicKeyInfo keys
// - Verify SHA-256 signatures in IEEE P1363 form, as WebCrypto produces them
//
// IMPORTANT NOTES:
// - Never throws on bad input: an unusable key or signature is simply not valid
// =========================================================
internal sealed class PlatformDeviceKeys : IPlatformDeviceKeys
{
    public bool IsValidPublicKey(string? publicKey)
    {
        return Import(publicKey) is { } key && Dispose(key);
    }

    public bool Verify(string publicKey, byte[] challenge, string? signature)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        if (string.IsNullOrWhiteSpace(signature) || Import(publicKey) is not { } key)
        {
            return false;
        }

        using (key)
        {
            try
            {
                return key.VerifyData(challenge, Convert.FromBase64String(signature), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }

    private static ECDsa? Import(string? publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            return null;
        }

        ECDsa key = ECDsa.Create();

        try
        {
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey.Trim()), out int read);

            if (read > 0 && key.KeySize == 256)
            {
                return key;
            }
        }
        catch (FormatException)
        {
        }
        catch (CryptographicException)
        {
        }

        key.Dispose();
        return null;
    }

    private static bool Dispose(ECDsa key)
    {
        key.Dispose();
        return true;
    }
}
