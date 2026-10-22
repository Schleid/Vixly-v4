using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Operations.Api.Hosting;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: OperationsApiTestHost.cs
//
// PURPOSE:
// Builds the real Operations.Api pipeline on a TestServer.
//
// RESPONSIBILITIES:
// - Register two callers: customer and a caller with another scope
// - Use the fake Platform and an unreachable database
//
// IMPORTANT NOTES:
// - Endpoints under test never touch the database
// =========================================================
internal sealed class OperationsApiTestHost : IAsyncDisposable
{
    public const string Host = "vixly-ops-internal.techcrafters.ltd";

    private readonly WebApplication _app;

    private OperationsApiTestHost(WebApplication app, FakePlatformClient platform, CallerTokenSigner customer, CallerTokenSigner other, FakeCustomerMailer mailer)
    {
        _app = app;
        Platform = platform;
        Customer = customer;
        Other = other;
        Mailer = mailer;
    }

    public FakeCustomerMailer Mailer { get; }

    public FakeCardGateway Cards { get; private init; } = new();

    public IServiceProvider Services => _app.Services;

    public HttpMessageHandler Handler() => _app.GetTestServer().CreateHandler();

    public FakePlatformClient Platform { get; }

    public CallerTokenSigner Customer { get; }

    public CallerTokenSigner Other { get; }

    public static async Task<OperationsApiTestHost> StartAsync(string? connectionString = null)
    {
        FakeCustomerMailer mailer = new();
        FakePlatformClient platform = new();
        FakeCardGateway cards = new();
        CallerTokenSigner customer = new("customer");
        CallerTokenSigner other = new("edge");
        WebApplication app = OperationsApiHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Operations.Api",
                "--AllowedHosts=*",
                "--OperationsFiles:Root=" + Path.Combine(Path.GetTempPath(), "vixly-files-" + Guid.NewGuid().ToString("N")),
                "--ConnectionStrings:Operations=" + (connectionString ?? "Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True")
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Services.RemoveAll<IOperationsPlatformClient>();
                builder.Services.AddSingleton<IOperationsPlatformClient>(platform);
                builder.Services.RemoveAll<IOperationsCustomerMailer>();
                builder.Services.AddSingleton<IOperationsCustomerMailer>(mailer);
                builder.Services.RemoveAll<IOperationsCardGateway>();
                builder.Services.AddSingleton<IOperationsCardGateway>(cards);
                builder.Services.RemoveAll<IOperationsPlanFeatures>();
                builder.Services.AddSingleton<IOperationsPlanFeatures>(new AllPlanFeatures());
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OperationsInternal:Hosts:0"] = Host,
                    ["OperationsInternal:Callers:customer:Scopes:0"] = "customer",
                    ["OperationsInternal:Callers:customer:Keys:k1"] = customer.PublicKey,
                    ["OperationsInternal:Callers:edge:Scopes:0"] = "tracking",
                    ["OperationsInternal:Callers:edge:Keys:k1"] = other.PublicKey
                });
            });

        await app.StartAsync();

        return new OperationsApiTestHost(app, platform, customer, other, mailer) { Cards = cards };
    }

    public HttpClient Client(string host = Host)
    {
        HttpClient client = _app.GetTestClient();
        client.BaseAddress = new Uri($"https://{host}/");
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        Customer.Dispose();
        Other.Dispose();
    }
}
