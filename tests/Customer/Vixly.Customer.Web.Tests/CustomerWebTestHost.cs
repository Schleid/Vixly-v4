using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Contracts.Operations.Internal;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Operations;

namespace Vixly.Customer.Web.Tests;

// =========================================================
// FILE: CustomerWebTestHost.cs
//
// PURPOSE:
// Builds the real Customer.Web pipeline on a TestServer.
//
// RESPONSIBILITIES:
// - Give Customer.Web a caller key and a fake Operations.Api
// - Use an unreachable database for the key ring
//
// IMPORTANT NOTES:
// - Pages under test never touch the database
// =========================================================
internal sealed class CustomerWebTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private readonly ECDsa _key;

    private CustomerWebTestHost(WebApplication app, ECDsa key, FakeOperationsHandler operations, FakeBotCheck bot)
    {
        _app = app;
        _key = key;
        Operations = operations;
        Bot = bot;
    }

    public FakeOperationsHandler Operations { get; }

    public FakeBotCheck Bot { get; }

    public static async Task<CustomerWebTestHost> StartAsync()
    {
        ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        FakeOperationsHandler operations = new(key);
        FakeBotCheck bot = new();
        WebApplication app = CustomerWebHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Customer.Web",
                "--AllowedHosts=*",
                "--ConnectionStrings:Customer=Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True",
                "--CustomerOperations:CallerKeyId=k1",
                "--CustomerOperations:CallerPrivateKey=" + Convert.ToBase64String(key.ExportPkcs8PrivateKey())
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Services.AddHttpClient(CustomerOperationsApi.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => operations);
                builder.Services.AddSingleton<Vixly.Customer.Web.Portal.ICustomerBotCheck>(bot);
            });

        await app.StartAsync();

        return new CustomerWebTestHost(app, key, operations, bot);
    }

    public void Tenant(string slug, string state, string? language = "en")
    {
        Operations.Tenants[slug] = new OperationsTenantHostDto(true, Guid.NewGuid(), state, "full", slug, language);
    }

    public HttpClient Client(string host)
    {
        return new HttpClient(_app.GetTestServer().CreateHandler()) { BaseAddress = new Uri($"https://{host}/") };
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _key.Dispose();
    }
}
