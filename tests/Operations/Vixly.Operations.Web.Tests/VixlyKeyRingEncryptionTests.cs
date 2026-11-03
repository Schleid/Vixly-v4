using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vixly.Shared.Hosting.Security;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: VixlyKeyRingEncryptionTests.cs
//
// PURPOSE:
// Proves the key ring kept in the database is locked with a key that is not in it (2026-11-03).
//
// RESPONSIBILITIES:
// - A new master key is stored locked: its value never appears in the stored XML
// - Another site with the same key reads the secrets; a site without it, or with another key, cannot
// - A previous key still opens keys locked before a change
// - At start-up a ring with only unlocked keys gets a locked key in use
//
// IMPORTANT NOTES:
// - In memory: no database, no files
// =========================================================
public sealed class VixlyKeyRingEncryptionTests
{
    private static readonly string KeyA = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static readonly string KeyB = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private sealed class MemoryRepository : IXmlRepository
    {
        public List<XElement> Elements { get; } = new();

        public IReadOnlyCollection<XElement> GetAllElements() => Elements.Select(element => new XElement(element)).ToList();

        public void StoreElement(XElement element, string friendlyName) => Elements.Add(new XElement(element));
    }

    private static ServiceProvider Site(MemoryRepository repository, string? key, string? previous = null)
    {
        Dictionary<string, string?> values = new() { [VixlyKeyRingEncryption.KeySetting] = key, [VixlyKeyRingEncryption.PreviousKeysSetting] = previous };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDataProtection().SetApplicationName("Vixly.Test");
        services.Configure<KeyManagementOptions>(options => options.XmlRepository = repository);
        services.AddVixlyKeyRingEncryption(configuration);
        return services.BuildServiceProvider();
    }

    private static string Protect(ServiceProvider site, string value) => site.GetRequiredService<IDataProtectionProvider>().CreateProtector("card").Protect(value);

    private static string Unprotect(ServiceProvider site, string value) => site.GetRequiredService<IDataProtectionProvider>().CreateProtector("card").Unprotect(value);

    [Fact]
    public void NewKeys_AreStoredLocked_AndOnlyTheSameKeyReadsThem()
    {
        MemoryRepository repository = new();
        using ServiceProvider web = Site(repository, KeyA);
        string protectedValue = Protect(web, "sk_live_secret");

        XElement stored = Assert.Single(repository.Elements);
        Assert.Contains(stored.Descendants(), element => element.Name.LocalName == "vixlyLockedKey");
        Assert.DoesNotContain(stored.Descendants(), element => element.Name.LocalName == "masterKey");

        using ServiceProvider api = Site(repository, KeyA);
        Assert.Equal("sk_live_secret", Unprotect(api, protectedValue));

        using ServiceProvider other = Site(repository, KeyB);
        Assert.ThrowsAny<CryptographicException>(() => Unprotect(other, protectedValue));

        using ServiceProvider none = Site(repository, null);
        Assert.ThrowsAny<CryptographicException>(() => Unprotect(none, protectedValue));
    }

    [Fact]
    public void APreviousKey_StillOpensWhatWasLockedBeforeAChange()
    {
        MemoryRepository repository = new();
        using ServiceProvider before = Site(repository, KeyA);
        string protectedValue = Protect(before, "whsec_123");

        using ServiceProvider after = Site(repository, KeyB, previous: KeyA);

        Assert.Equal("whsec_123", Unprotect(after, protectedValue));
    }

    [Fact]
    public async Task StartUp_GivesAnUnlockedRingALockedKeyInUse()
    {
        MemoryRepository repository = new();
        using ServiceProvider unlocked = Site(repository, null);
        string old = Protect(unlocked, "pickup-code");
        Assert.DoesNotContain(repository.Elements.SelectMany(element => element.Descendants()), element => element.Name.LocalName == "vixlyLockedKey");

        using ServiceProvider locked = Site(repository, KeyA);
        foreach (IHostedService service in locked.GetServices<IHostedService>())
        {
            await service.StartAsync(CancellationToken.None);
        }

        Assert.Equal(2, repository.Elements.Count);
        Assert.True(VixlyKeyRingLock.HasLockedKeyInUse(repository, locked.GetRequiredService<IKeyManager>().GetAllKeys(), DateTimeOffset.UtcNow));
        Assert.Equal("pickup-code", Unprotect(locked, old));
    }

    [Theory]
    [InlineData("not base64!")]
    [InlineData("AAAA")]
    public void AKeyThatIsNot32Bytes_IsRefused(string key)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [VixlyKeyRingEncryption.KeySetting] = key }).Build();

        Assert.Throws<InvalidOperationException>(() => VixlyKeyRingKeys.From(configuration));
    }
}
