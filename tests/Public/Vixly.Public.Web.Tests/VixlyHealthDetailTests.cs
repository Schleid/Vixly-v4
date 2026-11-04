using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Public.Web.Hosting;
using Vixly.Shared.Hosting;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: VixlyHealthDetailTests.cs
//
// PURPOSE:
// Proves the detailed health page every site shares (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Without the key, or with a wrong one: the status word only
// - With the key, in the query or the header: each check by name, with what is wrong
// - The settings check names a missing optional setting, and a waived one as waived
//
// IMPORTANT NOTES:
// - The public site carries the test because it has the lightest host
// =========================================================
public sealed class VixlyHealthDetailTests
{
    private const string Key = "health-detail-key-0123456789abcdef";

    [Fact]
    public async Task TheKey_ShowsEachCheck_AndWithoutItOnlyTheStatus()
    {
        await using WebApplication app = await StartAsync(Key, "PublicSite:CallerKeyId");
        HttpClient client = new(app.GetTestServer().CreateHandler()) { BaseAddress = new Uri("https://vixly.ltd") };

        string plain = await ReadAsync(client, "/health/ready");
        string wrong = await ReadAsync(client, "/health/ready?key=not-the-key-0123456789abcdef0123");
        string detail = await ReadAsync(client, "/health/ready?key=" + Key);
        HttpRequestMessage request = new(HttpMethod.Get, "/health/ready");
        request.Headers.Add(VixlyHealthDetail.KeyHeader, Key);
        string byHeader = await (await client.SendAsync(request)).Content.ReadAsStringAsync();

        Assert.DoesNotContain("\n", plain, StringComparison.Ordinal);
        Assert.Equal(plain, wrong);
        Assert.Contains("platform-api: ", detail, StringComparison.Ordinal);
        Assert.Contains("settings: ", detail, StringComparison.Ordinal);
        Assert.Contains("waived on purpose: PublicSite:CallerKeyId", detail, StringComparison.Ordinal);
        Assert.Contains("PublicSite:KeysPath (PublicSite__KeysPath): missing", detail, StringComparison.Ordinal);
        Assert.Contains("platform-api: ", byHeader, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AShortKey_TurnsTheDetailOff()
    {
        await using WebApplication app = await StartAsync("short", null);
        HttpClient client = new(app.GetTestServer().CreateHandler()) { BaseAddress = new Uri("https://vixly.ltd") };

        string detail = await ReadAsync(client, "/health/ready?key=short");

        Assert.DoesNotContain("platform-api", detail, StringComparison.Ordinal);
    }

    // Unhealthy answers 503 with the same body: read it whatever the status.
    private static async Task<string> ReadAsync(HttpClient client, string path) =>
        await (await client.GetAsync(path)).Content.ReadAsStringAsync();

    private static async Task<WebApplication> StartAsync(string key, string? waived)
    {
        List<string> args = new()
        {
            "--environment=Production",
            "--applicationName=Vixly.Public.Web",
            "--AllowedHosts=vixly.ltd",
            "--PublicSite:CanonicalBaseUrl=https://vixly.ltd",
            "--PublicSite:PlatformApiBaseUrl=https://platform.test/",
            "--VixlyHealth:DetailKey=" + key
        };
        if (waived is not null)
        {
            args.Add("--VixlySettingsCheck:Waived:0=" + waived);
        }

        WebApplication app = PublicSiteHost.Build(args.ToArray(), builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        });
        await app.StartAsync();
        return app;
    }
}
