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
// FILE: VixlyKeyRingDecryptor.cs
//
// PURPOSE:
// Opens a master key locked by VixlyKeyRingEncryptor.
//
// RESPONSIBILITIES:
// - Find the key-encryption key by its id among the current and previous ones
//
// IMPORTANT NOTES:
// - A key this site does not hold is refused: what it protects cannot be read
// =========================================================
public sealed class VixlyKeyRingDecryptor : IXmlDecryptor
{
    private readonly VixlyKeyRingKeys _keys;

    // Data Protection builds the decryptor itself, with the site's services.
    public VixlyKeyRingDecryptor(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _keys = services.GetRequiredService<VixlyKeyRingKeys>();
    }

    public VixlyKeyRingDecryptor(VixlyKeyRingKeys keys)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }

    public XElement Decrypt(XElement encryptedElement)
    {
        ArgumentNullException.ThrowIfNull(encryptedElement);
        string kek = (string?)encryptedElement.Attribute("kek") ?? string.Empty;

        if (!_keys.All.TryGetValue(kek, out byte[]? key))
        {
            throw new CryptographicException("This key ring entry is locked with a key this site does not have (" + VixlyKeyRingEncryption.KeySetting + ").");
        }

        byte[] nonce = Convert.FromBase64String((string?)encryptedElement.Element("nonce") ?? string.Empty);
        byte[] tag = Convert.FromBase64String((string?)encryptedElement.Element("tag") ?? string.Empty);
        byte[] cipher = Convert.FromBase64String((string?)encryptedElement.Element("value") ?? string.Empty);
        byte[] plain = new byte[cipher.Length];

        try
        {
            using AesGcm aes = new(key, tag.Length);
            aes.Decrypt(nonce, cipher, tag, plain, VixlyKeyRingEncryptor.Context);
            return XElement.Parse(Encoding.UTF8.GetString(plain));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }
}
