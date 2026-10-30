using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vixly.Operations.Web.Hosting;
using Vixly.Shared.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsDevelopmentProxyTests.cs
//
// PURPOSE:
// The developer computer's /customer proxy under double clicks and a portal that is not running (2026-10-18).
//
// RESPONSIBILITIES:
// - A request the browser abandons (a second click on a link or button) is no error, in either app
// - A portal that is not running gives a page that says so, not an exception
//
// IMPORTANT NOTES:
// - Real sockets (Kestrel), as on the developer computer; the in-memory test server never aborts
// =========================================================
public sealed class OperationsDevelopmentProxyTests
{
    [Fact]
    public async Task AbandonedRequests_ThroughTheProxy_AreNoError()
    {
        ConcurrentQueue<string> errors = new();
        await using WebApplication portal = await StartAsync(null, errors, "portal", app =>
        {
            app.UseVixlyClientDisconnects();
            app.MapGet("/customer/slow", async (HttpContext http) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), http.RequestAborted);
                return Results.Text("late");
            });
            app.MapPost("/customer/form", async (HttpContext http) =>
            {
                IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
                await Task.Delay(TimeSpan.FromSeconds(5), http.RequestAborted);
                return Results.Text(form["email"].ToString());
            });
            app.MapGet("/customer/sign-in", () => Results.Text("sign in"));
        });
        await using WebApplication operations = await StartAsync(Address(portal), errors, "operations", app => app.MapGet("/", () => "staff"));
        using HttpClient browser = new() { BaseAddress = new Uri(Address(operations)) };

        // The first click of a double click: the browser drops it as soon as the second one leaves.
        foreach (Func<CancellationToken, Task> click in new Func<CancellationToken, Task>[]
        {
            token => browser.GetAsync("/customer/slow", token),
            token => browser.PostAsync("/customer/form", new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = "ana@example.com" }), token)
        })
        {
            using CancellationTokenSource dropped = new(TimeSpan.FromMilliseconds(300));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => click(dropped.Token));
        }

        // The second click is answered.
        Assert.Equal("sign in", await browser.GetStringAsync("/customer/sign-in"));
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.True(errors.IsEmpty, string.Join("\n", errors));
    }

    [Fact]
    public async Task PortalNotRunning_AnswersWithAPage_NotAnException()
    {
        ConcurrentQueue<string> errors = new();
        await using WebApplication operations = await StartAsync("http://127.0.0.1:9", errors, "operations", app => app.MapGet("/", () => "staff"));
        using HttpClient browser = new() { BaseAddress = new Uri(Address(operations)) };

        HttpResponseMessage response = await browser.GetAsync("/customer/sign-in");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("Customer.Web", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.True(errors.IsEmpty, string.Join("\n", errors));
    }

    // portalUrl null: the portal itself; otherwise the staff app in front of it.
    private static async Task<WebApplication> StartAsync(string? portalUrl, ConcurrentQueue<string> errors, string name, Action<WebApplication> map)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development, ApplicationName = typeof(OperationsDevelopmentProxyTests).Assembly.GetName().Name });
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new ErrorCollector(errors, name));

        if (portalUrl is not null)
        {
            builder.Configuration[OperationsDevelopmentOptions.SectionName + ":CustomerPortalUrl"] = portalUrl;
            builder.Services.AddOperationsDevelopmentCustomerProxy(builder.Configuration);
        }

        WebApplication app = builder.Build();

        // Nothing may leave the app's own code unhandled: the debugger stops on it, and that looked like a crash (owner, 2026-10-18).
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                errors.Enqueue(name + ": escaped " + exception.GetType().Name + ": " + exception.Message);
                throw;
            }
        });

        if (portalUrl is not null)
        {
            app.UseOperationsDevelopmentCustomerProxy();
        }

        map(app);
        await app.StartAsync();
        return app;
    }

    private static string Address(WebApplication app) =>
        app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    private sealed class ErrorCollector : ILoggerProvider, ILogger
    {
        private readonly ConcurrentQueue<string> _errors;

        private readonly string _name;

        public ErrorCollector(ConcurrentQueue<string> errors, string name)
        {
            _errors = errors;
            _name = name;
        }

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                _errors.Enqueue(_name + ": " + formatter(state, exception) + " " + exception?.GetType().Name + ": " + exception?.Message);
            }
        }

        public void Dispose()
        {
        }
    }
}
