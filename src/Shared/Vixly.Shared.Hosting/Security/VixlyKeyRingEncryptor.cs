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
// FILE: VixlyKeyRingEncryptor.cs
//
// PURPOSE:
// Locks one Data Protection master key with AES-256-GCM before it is stored.
//
// RESPONSIBILITIES:
// - Write the locked key with the id of the key-encryption key that locked it
//
// IMPORTANT NOTES:
// - The master key's clear text is wiped from memory after use
// =========================================================
public sealed class VixlyKeyRingEncryptor : IXmlEncryptor
{
    internal const string ElementName = "vixlyLockedKey";

    internal static readonly byte[] Context = Encoding.UTF8.GetBytes("Vixly.KeyRing.v1");

    private readonly VixlyKeyRingKeys _keys;

    public VixlyKeyRingEncryptor(VixlyKeyRingKeys keys)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }

    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        ArgumentNullException.ThrowIfNull(plaintextElement);
        byte[] key = _keys.Current ?? throw new InvalidOperationException(VixlyKeyRingEncryption.KeySetting + " is not set.");
        byte[] plain = Encoding.UTF8.GetBytes(plaintextElement.ToString(SaveOptions.DisableFormatting));
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[16];

        try
        {
            using AesGcm aes = new(key, tag.Length);
            aes.Encrypt(nonce, plain, cipher, tag, Context);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }

        XElement locked = new(ElementName,
            new XAttribute("version", "1"),
            new XAttribute("kek", _keys.CurrentId!),
            new XElement("nonce", Convert.ToBase64String(nonce)),
            new XElement("tag", Convert.ToBase64String(tag)),
            new XElement("value", Convert.ToBase64String(cipher)));
        return new EncryptedXmlInfo(locked, typeof(VixlyKeyRingDecryptor));
    }
}
