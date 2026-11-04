using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Public.Web.Hosting;
using Vixly.Shared.Hosting.Settings;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: VixlySettingsCheckTests.cs
//
// PURPOSE:
// Proves the settings check every site runs before it starts (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - Each shape: missing, address, folder (inside the site, missing, fine), key, secret, template
// - A condition skips a rule; Development turns every problem into a warning
// - A site missing a required setting answers with the setup page, naming the setting and never a value
//
// IMPORTANT NOTES:
// - The check is shared by the six sites; the public site carries its tests because it has the lightest host
// =========================================================
public sealed class VixlySettingsCheckTests : IDisposable
{
    private readonly string _site = Directory.CreateTempSubdirectory("vixly-site-").FullName;

    private readonly string _outside = Directory.CreateTempSubdirectory("vixly-files-").FullName;

    [Fact]
    public void EveryShape_IsChecked_AndValuesNeverReachTheReport()
    {
        string key;
        using (ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            key = Convert.ToBase64String(ec.ExportPkcs8PrivateKey());
        }

        Directory.CreateDirectory(Path.Combine(_site, "uploads"));
        VixlySettingsReport report = Check(new Dictionary<string, string?>
        {
            ["Url:Good"] = "https://api.vixly.ltd",
            ["Url:Bad"] = "api.vixly.ltd",
            ["Folder:Inside"] = Path.Combine(_site, "uploads"),
            ["Folder:Missing"] = Path.Combine(_outside, "nowhere"),
            ["Folder:Good"] = _outside,
            ["Key:Good"] = key,
            ["Key:Bad"] = "not-a-key-secret-value",
            ["Secret:Short"] = "short-secret-value",
            ["Template:Bad"] = "https://portal.vixly.ltd"
        }, development: false,
            VixlySettingRule.Need("Text:Missing", VixlySettingKind.Text, "x"),
            VixlySettingRule.Need("Url:Good", VixlySettingKind.Url, "x"),
            VixlySettingRule.Need("Url:Bad", VixlySettingKind.Url, "x"),
            VixlySettingRule.Need("Folder:Inside", VixlySettingKind.Folder, "x"),
            VixlySettingRule.Need("Folder:Missing", VixlySettingKind.Folder, "x"),
            VixlySettingRule.Need("Folder:Good", VixlySettingKind.Folder, "x"),
            VixlySettingRule.Need("Key:Good", VixlySettingKind.EcPrivateKey, "x"),
            VixlySettingRule.Need("Key:Bad", VixlySettingKind.EcPrivateKey, "x"),
            VixlySettingRule.Should("Secret:Short", VixlySettingKind.Secret, "x", minLength: 32),
            VixlySettingRule.Need("Template:Bad", VixlySettingKind.Template, "x"),
            VixlySettingRule.Need("Skipped:Missing", VixlySettingKind.Text, "x", configuration => configuration["Never"] is not null));

        Assert.Equal(new[] { "Folder:Inside", "Folder:Missing", "Key:Bad", "Template:Bad", "Text:Missing", "Url:Bad", "Secret:Short" },
            report.Problems.Select(problem => problem.Rule.Key).ToArray());
        Assert.Contains("erase", report.Problems.Single(problem => problem.Rule.Key == "Folder:Inside").Problem, StringComparison.Ordinal);
        Assert.False(report.Problems.Single(problem => problem.Rule.Key == "Secret:Short").Blocks);
        Assert.DoesNotContain(report.Problems, problem => problem.ToString().Contains("secret-value", StringComparison.Ordinal));
        Assert.Equal("Text__Missing", report.Problems.Single(problem => problem.Rule.Key == "Text:Missing").Rule.EnvironmentName);
    }

    [Fact]
    public void Development_TurnsEveryProblemIntoAWarning()
    {
        VixlySettingsReport report = Check(new Dictionary<string, string?>(), development: true, VixlySettingRule.Need("Text:Missing", VixlySettingKind.Text, "x"));

        Assert.Empty(report.Blocking);
        Assert.Single(report.Warnings);
    }

    [Fact]
    public void AnOptionalSetting_CanBeWaivedOnPurpose_ButARequiredOneCannot()
    {
        VixlySettingsReport report = Check(
            new Dictionary<string, string?>
            {
                ["VixlySettingsCheck:Waived:0"] = "Payments__Stripe__SecretKey",
                ["VixlySettingsCheck:Waived:1"] = "Text:Required"
            },
            development: false,
            VixlySettingRule.Should("Payments:Stripe:SecretKey", VixlySettingKind.Secret, "x"),
            VixlySettingRule.Should("Mail:Password", VixlySettingKind.Text, "x"),
            VixlySettingRule.Need("Text:Required", VixlySettingKind.Text, "x"));

        Assert.Equal(new[] { "Payments:Stripe:SecretKey" }, report.Waived);
        Assert.Equal("Mail:Password", Assert.Single(report.Warnings).Rule.Key);
        Assert.Equal("Text:Required", Assert.Single(report.Blocking).Rule.Key);
    }

    [Fact]
    public async Task ASiteMissingARequiredSetting_AnswersWithTheSetupPage()
    {
        WebApplication app = PublicSiteHost.Build(
            new[]
            {
                "--environment=Production",
                "--applicationName=Vixly.Public.Web",
                "--AllowedHosts=vixly.ltd",
                "--PublicSite:CanonicalBaseUrl=https://vixly.ltd",
                "--PublicSite:PlatformApiBaseUrl="
            },
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            });
        await app.StartAsync();

        try
        {
            HttpClient client = new(app.GetTestServer().CreateHandler()) { BaseAddress = new Uri("https://vixly.ltd") };
            HttpResponseMessage page = await client.GetAsync("/en/features");
            string body = await page.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, page.StatusCode);
            Assert.Contains("PublicSite__PlatformApiBaseUrl", body, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    public void Dispose()
    {
        Directory.Delete(_site, recursive: true);
        Directory.Delete(_outside, recursive: true);
    }

    private VixlySettingsReport Check(Dictionary<string, string?> values, bool development, params VixlySettingRule[] rules)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return VixlySettingsCheck.Evaluate("test", configuration, _site, rules, development);
    }
}
