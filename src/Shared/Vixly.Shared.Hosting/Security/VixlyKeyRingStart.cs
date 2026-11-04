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
// FILE: VixlyKeyRingStart.cs
//
// PURPOSE:
// At start-up, makes sure a locked master key is in use.
//
// RESPONSIBILITIES:
// - Make a new locked key, active now, when the ring has none in use yet
//
// IMPORTANT NOTES:
// - Never stops the site: a problem is logged and the settings check names what to fix
// =========================================================
internal sealed class VixlyKeyRingStart : IHostedService
{
    private readonly IKeyManager _keys;

    private readonly IOptions<KeyManagementOptions> _options;

    private readonly TimeProvider _time;

    private readonly ILogger<VixlyKeyRingStart> _logger;

    public VixlyKeyRingStart(IKeyManager keys, IOptions<KeyManagementOptions> options, ILogger<VixlyKeyRingStart> logger, TimeProvider? time = null)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _time = time ?? TimeProvider.System;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset now = _time.GetUtcNow();

            if (!VixlyKeyRingLock.HasLockedKeyInUse(_options.Value.XmlRepository, _keys.GetAllKeys(), now))
            {
                _keys.CreateNewKey(now, now.Add(_options.Value.NewKeyLifetime));
                _logger.LogInformation("Data Protection: a new key locked with the key-encryption key is now in use.");
            }
        }
        catch (Exception exception) when (exception is CryptographicException or InvalidOperationException)
        {
            // The site still starts; the settings check and the log say what to fix.
            _logger.LogError(exception, "Data Protection: the key ring could not be checked or locked.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
