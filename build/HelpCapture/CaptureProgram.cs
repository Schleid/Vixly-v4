using Microsoft.AspNetCore.DataProtection;
using Vixly.Public.Web.Hosting;
using Vixly.Public.Web.Platform;

namespace Vixly.HelpCapture;

// =========================================================
// FILE: CaptureProgram.cs
//
// PURPOSE:
// Runs real Public pages with fictional, local-only capture data.
//
// RESPONSIBILITIES:
// - Isolate screenshots from production services and browser data.
//
// IMPORTANT NOTES:
// - Tool only; never deployed or registered in Public.Web.
// - Listen only on loopback; do not read user secrets.
// =========================================================
public static class CaptureProgram
{
    public static async Task Main()
    {
        string root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "AGENTS.md"))) root = Directory.GetParent(root)!.FullName;
        WebApplication app = PublicSiteHost.Build(new[]
        {
            "--environment=HelpCapture", "--applicationName=Vixly.Public.Web",
            "--contentRoot=" + Path.Combine(root, "src/Public/Vixly.Public.Web"),
            "--urls=https://localhost:7443", "--AllowedHosts=localhost",
            "--VixlySettingsCheck:Enabled=false", "--PublicSite:RegistrationOpen=true",
            "--PublicSite:PlatformApiBaseUrl=https://capture.invalid/",
            "--PublicSite:CanonicalBaseUrl=https://localhost:7443",
            "--PublicSite:SalesEmail=sales@bluewater-shipping.example",
            "--PublicSite:TurnstileSiteKey="
        }, builder =>
        {
            builder.WebHost.UseStaticWebAssets();
            builder.Configuration["PublicSite:CallerPrivateKey"] = string.Empty;
            builder.Configuration["PublicSite:CallerKeyId"] = string.Empty;
            builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            builder.Services.AddSingleton<IStartupFilter, CaptureScenario>();
            builder.Services.AddHttpClient<PlatformApi>().ConfigurePrimaryHttpMessageHandler(() => new CaptureApiHandler());
        });
        await app.RunAsync();
    }
}
