using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Api.Hosting;

namespace Vixly.Platform.Api.Tests;

// =========================================================
// FILE: PlatformApiTestHost.cs
//
// PURPOSE:
// Builds the real Platform.Api pipeline on a TestServer.
//
// RESPONSIBILITIES:
// - Use the production host name and an unreachable database
// - Use an in-memory Data Protection key ring
// - Turn the bot check on with no secret, so it fails closed
//
// IMPORTANT NOTES:
// - Only endpoints that refuse before touching the database are
//   called; database behaviour is proven in Infrastructure tests
// =========================================================
internal static class PlatformApiTestHost
{
    public const string Host = "vixly-api.techcrafters.ltd";

    public static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        WebApplication app = PlatformApiHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Platform.Api",
                "--ConnectionStrings:Platform=Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True"
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PlatformBotCheck:Enabled"] = "true",
                    ["PlatformBotCheck:SecretKey"] = string.Empty
                });
            });

        await app.StartAsync();
        HttpClient client = app.GetTestClient();
        client.BaseAddress = new Uri($"https://{Host}/");

        return (app, client);
    }
}
