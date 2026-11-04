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
// FILE: VixlyKeyRingLock.cs
//
// PURPOSE:
// Tells whether a key active now was stored locked.
//
// RESPONSIBILITIES:
// - Read the stored key elements and match their ids
//
// IMPORTANT NOTES:
// - No repository (in memory): nothing to lock
// =========================================================
public static class VixlyKeyRingLock
{
    // True when a key that is active now was stored locked.
    public static bool HasLockedKeyInUse(IXmlRepository? repository, IReadOnlyCollection<IKey> keys, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(keys);

        if (repository is null)
        {
            return true;
        }

        HashSet<Guid> locked = repository.GetAllElements()
            .Where(element => element.Name.LocalName == "key" && element.Descendants().Any(child => child.Name.LocalName == VixlyKeyRingEncryptor.ElementName))
            .Select(element => Guid.TryParse((string?)element.Attribute("id"), out Guid id) ? id : Guid.Empty)
            .ToHashSet();

        return keys.Any(key => !key.IsRevoked && key.ActivationDate <= now && key.ExpirationDate > now && locked.Contains(key.KeyId));
    }
}
