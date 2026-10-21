using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Public.Web.Hosting;
using Vixly.Public.Web.Platform;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicSiteTestHost.cs
//
// PURPOSE:
// Runs the real public site pipeline in memory with a stand-in Platform.Api.
//
// RESPONSIBILITIES:
// - Build the host as production does, on a test server, over HTTPS, at the production host name
// - Replace only the Platform.Api transport (StubPlatformState) and the key ring
// - Sign caller tokens with a fresh key whose public half the tests can check
//
// IMPORTANT NOTES:
// - Sign-up is closed unless a test opens it, as in production (D-U4)
// =========================================================
internal sealed class PublicSiteTestHost : IAsyncDisposable
{
    public const string BaseAddress = "https://vixly.techcrafters.ltd";

    private readonly WebApplication _app;

    private PublicSiteTestHost(WebApplication app, StubPlatformState platform, ECDsa callerKey)
    {
        _app = app;
        Platform = platform;
        CallerKey = callerKey;
    }

    public StubPlatformState Platform { get; }

    public ECDsa CallerKey { get; }

    public static async Task<PublicSiteTestHost> StartAsync(bool registrationOpen = false)
    {
        StubPlatformState platform = new();
        ECDsa callerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        WebApplication app = PublicSiteHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Public.Web",
                "--AllowedHosts=vixly.techcrafters.ltd",
                "--PublicSite:PlatformApiBaseUrl=https://platform.test/",
                "--PublicSite:CanonicalBaseUrl=" + BaseAddress,
                "--PublicSite:RegistrationOpen=" + (registrationOpen ? "true" : "false"),
                "--PublicSite:CallerKeyId=public-test",
                "--PublicSite:CallerPrivateKey=" + Convert.ToBase64String(callerKey.ExportPkcs8PrivateKey()),
                "--PublicSite:CheckoutOrigins:0=https://checkout.stripe.com",
                "--PublicSite:SalesEmail=sales@example.com"
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IStartupFilter, VisitorAddressStartupFilter>();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Services.AddHttpClient<PlatformApi>().ConfigurePrimaryHttpMessageHandler(() => new StubPlatformHandler(platform));
            });

        await app.StartAsync();

        return new PublicSiteTestHost(app, platform, callerKey);
    }

    public HttpClient Client()
    {
        return new HttpClient(_app.GetTestServer().CreateHandler()) { BaseAddress = new Uri(BaseAddress) };
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        CallerKey.Dispose();
    }
}
