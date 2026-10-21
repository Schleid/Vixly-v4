using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Setup;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsWebTestHost.cs
//
// PURPOSE:
// Builds the real Operations.Web pipeline on a TestServer.
//
// RESPONSIBILITIES:
// - Use the fake Platform and mailer, and a test database or an unreachable one
// - Stop the background workers
// - Run the owner invitation as the outbox would
//
// IMPORTANT NOTES:
// - Pages that read the database need the database fixture
// =========================================================
internal sealed class OperationsWebTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private OperationsWebTestHost(WebApplication app, FakePlatformClient platform, FakeIdentityMailer mailer, FakeDeviceClient devices, FakeLegalClient legal, FakeCompanyClient company, FakePlanFeatures features)
    {
        _app = app;
        Platform = platform;
        Mailer = mailer;
        Devices = devices;
        Legal = legal;
        Company = company;
        Features = features;
    }

    public FakePlatformClient Platform { get; }

    public FakeIdentityMailer Mailer { get; }

    public FakeDeviceClient Devices { get; }

    public FakeLegalClient Legal { get; }

    public FakeCompanyClient Company { get; }

    public FakePlanFeatures Features { get; }

    public IServiceProvider Services => _app.Services;

    public FakeCustomerMailer CustomerMailer { get; private init; } = new();

    public FakeInspectionDraw Draw { get; private init; } = new();

    public static async Task<OperationsWebTestHost> StartAsync(string? connectionString = null)
    {
        FakePlatformClient platform = new();
        FakeIdentityMailer mailer = new();
        FakeDeviceClient devices = new();
        FakeLegalClient legal = new();
        FakeCompanyClient company = new();
        FakePlanFeatures features = new();
        FakeCustomerMailer customerMailer = new();
        FakeInspectionDraw draw = new();
        WebApplication app = OperationsWebHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Operations.Web",
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
                builder.Services.RemoveAll<IOperationsLegalClient>();
                builder.Services.AddSingleton<IOperationsLegalClient>(legal);
                builder.Services.RemoveAll<IOperationsCompanyClient>();
                builder.Services.AddSingleton<IOperationsCompanyClient>(company);
                builder.Services.RemoveAll<IOperationsPlanFeatures>();
                builder.Services.AddSingleton<IOperationsPlanFeatures>(features);
                builder.Services.RemoveAll<IOperationsDeviceClient>();
                builder.Services.AddSingleton<IOperationsDeviceClient>(devices);
                builder.Services.RemoveAll<IOperationsIdentityMailer>();
                builder.Services.AddSingleton<IOperationsIdentityMailer>(mailer);
                builder.Services.RemoveAll<IOperationsCustomerMailer>();
                builder.Services.AddSingleton<IOperationsCustomerMailer>(customerMailer);
                builder.Services.RemoveAll<Vixly.Operations.Application.Receiving.OperationsInspectionDraw>();
                builder.Services.AddSingleton<Vixly.Operations.Application.Receiving.OperationsInspectionDraw>(draw);
                builder.Services.RemoveAll<IHostedService>();
                builder.Services.AddScoped<OperationsDemoStaffSeeder>();
            });

        await app.StartAsync();

        return new OperationsWebTestHost(app, platform, mailer, devices, legal, company, features) { CustomerMailer = customerMailer, Draw = draw };
    }

    public HttpClient Client(string host)
    {
        return new HttpClient(_app.GetTestServer().CreateHandler()) { BaseAddress = new Uri($"https://{host}/") };
    }

    public CookieClient Browser(string host)
    {
        return new CookieClient(_app.GetTestServer().CreateHandler(), new Uri($"https://{host}/"));
    }

    // What the outbox worker does when Platform says the account is ready for setup.
    public async Task InviteOwnerAsync(Guid tenantId)
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenantId);
        IOperationsOutboxHandler handler = scope.ServiceProvider.GetServices<IOperationsOutboxHandler>().Single(item => item.MessageType == OperationsSetupMessages.OwnerInvitationRequested);
        await handler.HandleAsync(OperationsOutboxMessage.Create(OperationsSetupMessages.OwnerInvitationRequested, tenantId, tenantId, "{}", DateTimeOffset.UtcNow), CancellationToken.None);
    }

    // Runs the Development demo seed once, as Operations.Web does at start-up.
    public async Task RunDemoSeedAsync(params string[] slugs)
    {
        OperationsDemoDataWorker worker = new(
            _app.Services.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new OperationsDemoDataOptions { Slugs = slugs, WaitForTenantsMinutes = 0 }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OperationsDemoDataWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!;
    }

    // What the Platform feed does when a tenant's status, access or Terms change.
    public Task ForgetTenantAsync(Guid tenantId)
    {
        _app.Services.GetRequiredService<IOperationsTenantDirectory>().Forget(tenantId);
        return Task.CompletedTask;
    }

    // Forgets the device checks kept for 5 minutes, as if the time had passed.
    public void ClearDeviceChecks()
    {
        if (_app.Services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>() is Microsoft.Extensions.Caching.Memory.MemoryCache cache)
        {
            cache.Compact(1.0);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
