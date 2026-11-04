using System.Security.Cryptography;

namespace Vixly.Shared.Kernel.Security;

// =========================================================
// FILE: VixlyCallerKey.cs
//
// PURPOSE:
// Reads the private key a site signs its calls to another site
// with (CallerPrivateKey), in either of its two written forms.
//
// RESPONSIBILITIES:
// - 64 hexadecimal characters: the P-256 private number itself
//   (production 2026-10-24: the hosting panel damaged base64 keys,
//   whose + / = it does not keep; hex has only 0-9 and a-f)
// - Base64 PKCS#8: the form printed before, still accepted
// - Refuse anything else, and any curve but P-256
//
// IMPORTANT NOTES:
// - Spaces and line breaks inside the value are ignored (a key
//   copied from a narrow console can arrive wrapped)
// - Never puts the value in an exception message
// =========================================================
public static class VixlyCallerKey
{
    public const int HexLength = 64;

    public static ECDsa Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string text = string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
        ECDsa key = ECDsa.Create();

        try
        {
            if (text.Length == HexLength && text.All(Uri.IsHexDigit))
            {
                key.ImportParameters(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = Convert.FromHexString(text) });
            }
            else
            {
                key.ImportPkcs8PrivateKey(Convert.FromBase64String(text), out _);
            }

            if (key.KeySize != 256)
            {
                throw new CryptographicException("The caller key is not a P-256 key.");
            }

            return key;
        }
        catch
        {
            key.Dispose();
            throw;
        }
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            using ECDsa key = Import(value);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return false;
        }
    }

    // The 64-character form, for the key tool.
    public static string ToHex(ECDsa key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return Convert.ToHexString(key.ExportParameters(true).D!).ToLowerInvariant();
    }
}
