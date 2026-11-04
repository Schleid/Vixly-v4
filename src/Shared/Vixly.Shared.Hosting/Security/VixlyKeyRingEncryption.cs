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
// FILE: VixlyKeyRingEncryption.cs
//
// PURPOSE:
// Locks the Data Protection key ring that sits in the database with a key that is not in the database (2026-11-03).
//
// RESPONSIBILITIES:
// - Encrypt each new master key with AES-256-GCM under DataProtection:KeyEncryptionKey (environment variable)
// - Read master keys encrypted under the current key or one of DataProtection:PreviousKeyEncryptionKeys
// - When the key ring has no locked key in use yet, make a new one at start-up, so new secrets are never kept under an unlocked key
//
// IMPORTANT NOTES:
// - Without it, a copy of the database (a .bak, a stolen login) is enough to read every secret the site keeps:
//   companies' card and text provider keys, webhook secrets, authenticator keys
// - Every site that shares a key ring needs the same key: Operations Web and Operations API; Control and Platform API (the customer portal has its own ring)
// - Without the setting (Development) nothing changes: keys stay as before
// - Older unlocked keys stay readable, so what they protected still reads; new secrets use the locked key
// - The key: 32 random bytes, base64 (44 characters); never in a file, never logged
// =========================================================
public static class VixlyKeyRingEncryption
{
    public const string KeySetting = "DataProtection:KeyEncryptionKey";

    public const string PreviousKeysSetting = "DataProtection:PreviousKeyEncryptionKeys";

    // The length of a base64 32-byte key, for the settings check.
    public const int KeyTextLength = 44;

    public static IServiceCollection AddVixlyKeyRingEncryption(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        VixlyKeyRingKeys keys = VixlyKeyRingKeys.From(configuration);
        services.AddSingleton(keys);

        if (keys.Current is null)
        {
            return services;
        }

        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(provider => new ConfigureOptions<KeyManagementOptions>(options =>
            options.XmlEncryptor = new VixlyKeyRingEncryptor(keys)));
        services.AddHostedService<VixlyKeyRingStart>();
        return services;
    }
}
