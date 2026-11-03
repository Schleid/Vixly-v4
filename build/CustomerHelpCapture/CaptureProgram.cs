using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Vixly.Customer.Web.Hosting;
using Vixly.Customer.Web.Operations;

namespace Vixly.CustomerHelpCapture;

// =========================================================
// FILE: CaptureProgram.cs
// PURPOSE:
// Runs the real Customer app with fictional, local capture data.
// RESPONSIBILITIES:
// - Keep all data, signing keys and browser sessions temporary.
// IMPORTANT NOTES:
// - Loopback only; no user secrets, real API, database or payments.
// =========================================================
public static class CaptureProgram
{
    public static async Task Main(string[] args)
    {
        string root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "AGENTS.md"))) root = Directory.GetParent(root)!.FullName;
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WebApplication app = CustomerWebHost.Build(new[]
        {
            "--environment=HelpCapture", "--applicationName=Vixly.Customer.Web", "--urls=https://localhost:7444",
            "--contentRoot=" + Path.Combine(root, "src/Customer/Vixly.Customer.Web"), "--AllowedHosts=*",
            "--VixlySettingsCheck:Enabled=false", "--TenantHosting:BaseDomains:0=localhost",
            "--ConnectionStrings:Customer=Server=127.0.0.1,1;Database=None;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True",
            "--CustomerOperations:BaseUrl=https://capture.invalid", "--CustomerOperations:CallerKeyId=capture",
            "--CustomerOperations:CallerPrivateKey=" + Convert.ToBase64String(key.ExportPkcs8PrivateKey())
        }, builder =>
        {
            builder.WebHost.UseStaticWebAssets();
            builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            // Do not warm the unused database-backed key ring; antiforgery uses the ephemeral provider.
            foreach (var service in builder.Services.Where(item => item.ServiceType == typeof(IHostedService)
                && item.ImplementationType?.Name == "DataProtectionHostedService").ToArray())
                builder.Services.Remove(service);
            builder.Services.AddSingleton<IStartupFilter, CaptureStartup>();
            builder.Services.AddHttpClient(CustomerOperationsApi.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new CaptureApiHandler(args.Contains("--tour", StringComparer.Ordinal)));
        });
        await app.RunAsync();
    }
}
