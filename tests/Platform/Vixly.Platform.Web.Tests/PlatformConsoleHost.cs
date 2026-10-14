using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vixly.Platform.Web.Hosting;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformConsoleHost.cs
//
// PURPOSE:
// Runs the real console pipeline in memory for HTTP tests.
//
// RESPONSIBILITIES:
// - Build the host exactly as production does, on a test
//   server, over HTTPS, at the production host name (the
//   allowed-hosts list applies)
// - Replace only what needs a database: the key ring (in
//   memory) and the start-up role synchronisation (removed)
//
// IMPORTANT NOTES:
// - The connection string points nowhere; a test that reaches
//   the database fails fast, so these tests stay on the
//   pipeline: headers, antiforgery, rate limits, redirects
// =========================================================
internal sealed class PlatformConsoleHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private PlatformConsoleHost(WebApplication app)
    {
        _app = app;
    }

    public static async Task<PlatformConsoleHost> StartAsync()
    {
        WebApplication app = PlatformWebHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Platform.Web",
                "--ConnectionStrings:Platform=Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True",
                "--PlatformOutbox:Enabled=false",
                "--PlatformSubscriptionClock:Enabled=false"
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());

                ServiceDescriptor? startup = builder.Services.FirstOrDefault(service =>
                    service.ServiceType == typeof(IHostedService)
                    && service.ImplementationType == typeof(PlatformSystemRoleStartup));

                if (startup is not null)
                {
                    builder.Services.Remove(startup);
                }
            });

        await app.StartAsync();

        return new PlatformConsoleHost(app);
    }

    public HttpClient Client(bool followRedirects = false)
    {
        HttpClient client = _app.GetTestServer().CreateClient();
        client.BaseAddress = new Uri("https://vixly-control.techcrafters.ltd");

        return followRedirects
            ? client
            : new HttpClient(_app.GetTestServer().CreateHandler()) { BaseAddress = new Uri("https://vixly-control.techcrafters.ltd") };
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
