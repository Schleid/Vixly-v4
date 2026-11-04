using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vixly.Shared.Hosting.Security;

// =========================================================
// FILE: VixlyKeyRingKeys.cs
//
// PURPOSE:
// The key-encryption keys a site holds: the current one and older ones.
//
// RESPONSIBILITIES:
// - Read DataProtection:KeyEncryptionKey and DataProtection:PreviousKeyEncryptionKeys
// - Name each key by a short public id (never the key)
//
// IMPORTANT NOTES:
// - 32 random bytes in base64; anything else stops the site with the setting's name
// =========================================================
public sealed class VixlyKeyRingKeys
{
    private VixlyKeyRingKeys(byte[]? current, IReadOnlyList<byte[]> previous)
    {
        Current = current;
        CurrentId = current is null ? null : Id(current);
        All = (current is null ? previous : previous.Prepend(current)).ToDictionary(Id, key => key, StringComparer.Ordinal);
    }

    public byte[]? Current { get; }

    public string? CurrentId { get; }

    public IReadOnlyDictionary<string, byte[]> All { get; }

    public static VixlyKeyRingKeys From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        byte[]? current = Parse(configuration[VixlyKeyRingEncryption.KeySetting], VixlyKeyRingEncryption.KeySetting);
        List<byte[]> previous = new();

        foreach (string text in (configuration[VixlyKeyRingEncryption.PreviousKeysSetting] ?? string.Empty).Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            previous.Add(Parse(text, VixlyKeyRingEncryption.PreviousKeysSetting)!);
        }

        return new VixlyKeyRingKeys(current, previous);
    }

    // A short public name for a key (never the key): the first 16 hex characters of its SHA-256.
    public static string Id(byte[] key) => Convert.ToHexStringLower(SHA256.HashData(key))[..16];

    private static byte[]? Parse(string? text, string setting)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            byte[] key = Convert.FromBase64String(text.Trim());

            if (key.Length == 32)
            {
                return key;
            }
        }
        catch (FormatException)
        {
        }

        throw new InvalidOperationException(setting + " must be 32 random bytes in base64 (44 characters).");
    }
}
