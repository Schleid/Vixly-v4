using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vixly.Shared.Hosting.Diagnostics;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: VixlyRequestTimingTests.cs
//
// PURPOSE:
// Proves the request timing of the performance plan (P-1, owner 2026-11-04).
//
// RESPONSIBILITIES:
// - Database commands and outgoing calls are counted for the request that made them
// - The header shows only with the setting on and only to whom the site allows
// - A slow request is logged with its route pattern, never its address or query
//
// IMPORTANT NOTES:
// - A small app in memory; the database event is raised the way Entity Framework raises it
// =========================================================
public sealed class VixlyRequestTimingTests
{
    [Fact]
    public async Task Header_CountsSqlAndCalls_OnlyWhenOnAndAllowed()
    {
        await using WebApplication app = await StartAsync(showHeader: true, slowMs: 0, allowed: http => http.Request.Headers.ContainsKey("X-Owner"));
        HttpClient client = app.GetTestClient();

        HttpRequestMessage owner = new(HttpMethod.Get, "/staff/thing/42?token=secret");
        owner.Headers.Add("X-Owner", "1");
        HttpResponseMessage shown = await client.SendAsync(owner);
        string timing = string.Join(",", shown.Headers.GetValues("Server-Timing"));
        Assert.Contains("sql;dur=", timing, StringComparison.Ordinal);
        Assert.Contains("desc=\"2\"", timing, StringComparison.Ordinal);
        Assert.Contains("calls;dur=", timing, StringComparison.Ordinal);
        Assert.Contains("desc=\"1\"", timing, StringComparison.Ordinal);

        Assert.False((await client.GetAsync("/staff/thing/42")).Headers.Contains("Server-Timing"));

        await using WebApplication off = await StartAsync(showHeader: false, slowMs: 0, allowed: _ => true);
        Assert.False((await off.GetTestClient().GetAsync("/staff/thing/42")).Headers.Contains("Server-Timing"));
    }

    [Fact]
    public async Task SlowRequest_IsLoggedWithItsRoute_NeverItsAddress()
    {
        Captured logs = new();
        await using WebApplication app = await StartAsync(showHeader: false, slowMs: 1, allowed: null, logs);
        HttpResponseMessage response = await app.GetTestClient().GetAsync("/staff/thing/42?token=secret");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        string line = Assert.Single(logs.Lines, item => item.Contains("Slow request", StringComparison.Ordinal));
        Assert.Contains("/staff/thing/{id}", line, StringComparison.Ordinal);
        Assert.Contains("2 SQL commands", line, StringComparison.Ordinal);
        Assert.Contains("1 calls", line, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", line, StringComparison.Ordinal);
        Assert.DoesNotContain("/42", line, StringComparison.Ordinal);

        // Off when the threshold is 0.
        Captured quiet = new();
        await using WebApplication none = await StartAsync(showHeader: false, slowMs: 0, allowed: null, quiet);
        await none.GetTestClient().GetAsync("/staff/thing/42");
        Assert.DoesNotContain(quiet.Lines, item => item.Contains("Slow request", StringComparison.Ordinal));
    }

    private static async Task<WebApplication> StartAsync(bool showHeader, int slowMs, Func<HttpContext, bool>? allowed, Captured? logs = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // The test folder holds Operations' appsettings.json; this small app sets its own values.
            ["AllowedHosts"] = "*",
            ["VixlyTiming:ShowHeader"] = showHeader ? "true" : "false",
            ["VixlyTiming:SlowRequestMs"] = slowMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        builder.Logging.ClearProviders();

        if (logs is not null)
        {
            builder.Logging.AddProvider(logs);
        }

        builder.Services.AddVixlyRequestTiming(builder.Configuration);
        builder.Services.AddHttpClient("other").ConfigurePrimaryHttpMessageHandler(() => new Answer());
        WebApplication app = builder.Build();
        app.UseVixlyRequestTiming(allowed is null ? null : http => Task.FromResult(allowed(http)));
        app.MapGet("/staff/thing/{id}", async (HttpContext http, IHttpClientFactory factory) =>
        {
            // Two commands as Entity Framework reports them, then one call to another site.
            using DiagnosticListener ef = new("Microsoft.EntityFrameworkCore");
            for (int index = 0; index < 2; index++)
            {
                ef.Write("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted", new { Duration = TimeSpan.FromMilliseconds(3) });
            }

            await factory.CreateClient("other").GetAsync("https://other.test/");
            await Task.Delay(5);
            return "ok";
        });
        await app.StartAsync();
        return app;
    }

    private sealed class Answer : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private sealed class Captured : ILoggerProvider, ILogger
    {
        public ConcurrentBag<string> Lines { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add(formatter(state, exception));

        public void Dispose()
        {
        }
    }
}
