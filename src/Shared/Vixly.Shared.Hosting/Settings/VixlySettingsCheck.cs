using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Vixly.Shared.Hosting.Settings;

// =========================================================
// FILE: VixlySettingsCheck.cs
//
// PURPOSE:
// Checks the settings of a site before it starts, so a missing setting names itself instead of a bare start-up failure (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - Check every rule of the site against its configuration
// - Missing required settings outside Development: start a setup page instead of the site (503, the names to set), and log them
// - Otherwise register the report, the start-up log and the health check
//
// IMPORTANT NOTES:
// - Called right after the builder is created, before anything that would throw on a missing setting
// - Never reads a value into a message: names and reasons only
// - In Development every problem is a warning: local defaults and user secrets fill the gaps
// - A folder inside the site folder is refused: an update that replaces the site would erase it
// - VixlySettingsCheck:Waived lists optional settings the owner goes without on purpose (2026-10-25):
//   they no longer degrade the health check. A required setting cannot be waived: the site would fail later
// =========================================================
public static class VixlySettingsCheck
{
    public static VixlySettingsReport Evaluate(string site, IConfiguration configuration, string contentRoot, IReadOnlyList<VixlySettingRule> rules, bool development)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(rules);

        List<VixlySettingProblem> problems = new();
        List<string> waived = new();
        HashSet<string> waivers = Waivers(configuration);

        foreach (VixlySettingRule rule in rules)
        {
            if (rule.When is not null && !rule.When(configuration))
            {
                continue;
            }

            if (Check(rule, configuration, contentRoot) is { } problem)
            {
                if (!rule.Required && waivers.Contains(rule.Key))
                {
                    waived.Add(rule.Key);
                    continue;
                }

                problems.Add(new VixlySettingProblem(development ? rule with { Required = false } : rule, problem));
            }
        }

        return new VixlySettingsReport(site, problems, waived);
    }

    // Returns the setup application when the site cannot start; null when it can (and the report is registered).
    public static WebApplication? Run(WebApplicationBuilder builder, string site, IReadOnlyList<VixlySettingRule> rules, Action<WebApplicationBuilder>? configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Test hosts that run with only the settings their feature needs turn the check off (VixlySettingsCheck:Enabled=false).
        VixlySettingsReport report = string.Equals(builder.Configuration["VixlySettingsCheck:Enabled"]?.Trim(), "false", StringComparison.OrdinalIgnoreCase)
            ? new VixlySettingsReport(site, Array.Empty<VixlySettingProblem>())
            : Evaluate(site, builder.Configuration, builder.Environment.ContentRootPath, Common.Concat(rules).ToArray(), builder.Environment.IsDevelopment());

        if (report.Blocking.Count == 0)
        {
            builder.Services.AddSingleton(report);
            builder.Services.AddHostedService<VixlySettingsLog>();
            builder.Services.AddHealthChecks().AddCheck<VixlySettingsHealthCheck>("settings", tags: new[] { VixlyHealth.ReadyTag });
            return null;
        }

        configure?.Invoke(builder);
        WebApplication app = builder.Build();
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Vixly.Settings");

        foreach (VixlySettingProblem problem in report.Problems)
        {
            logger.Log(problem.Blocks ? LogLevel.Critical : LogLevel.Warning, "Setting {Problem}", problem.ToString());
        }

        logger.LogCritical("{Site} is not started: {Count} required setting(s) are missing or wrong. Every request answers with the setup page.", site, report.Blocking.Count);

        string page = SetupPage(report);
        app.MapGet("/health/live", () => Results.Text("Healthy"));
        app.MapFallback(async http =>
        {
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            http.Response.Headers.CacheControl = "no-store";
            http.Response.ContentType = "text/html; charset=utf-8";
            await http.Response.WriteAsync(page);
        });

        return app;
    }

    // VixlySettingsCheck:Waived:0, :1, ... (environment: VixlySettingsCheck__Waived__0). A key may be written with ":" or "__".
    public const string WaivedSection = "VixlySettingsCheck:Waived";

    private static HashSet<string> Waivers(IConfiguration configuration) =>
        configuration.GetSection(WaivedSection).GetChildren()
            .Select(child => child.Value?.Trim().Replace("__", ":", StringComparison.Ordinal))
            .Where(key => !string.IsNullOrEmpty(key))
            .Select(key => key!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Rules every site shares.
    private static readonly VixlySettingRule[] Common =
    {
        VixlySettingRule.Should("AllowedHosts", VixlySettingKind.Text, "The site's own host names; \"*\" accepts any host.", configuration => configuration["AllowedHosts"]?.Trim() is null or "" or "*"),
        VixlySettingRule.Need("Hosting:EdgeSecret", VixlySettingKind.Secret, "The secret the Cloudflare Worker sends, needed once Hosting:EdgeHosts is set.",
            configuration => !string.IsNullOrWhiteSpace(configuration["Hosting:EdgeHosts:0"]), 32)
    };

    private static string? Check(VixlySettingRule rule, IConfiguration configuration, string contentRoot)
    {
        if (rule.Kind == VixlySettingKind.Section)
        {
            return configuration.GetSection(rule.Key).GetChildren().Any() ? null : "missing";
        }

        if (rule.Kind == VixlySettingKind.List)
        {
            return configuration.GetSection(rule.Key).GetChildren().Any(child => !string.IsNullOrWhiteSpace(child.Value)) ? null : "missing";
        }

        string? value = configuration[rule.Key]?.Trim();

        if (rule.Key == "AllowedHosts")
        {
            return string.IsNullOrEmpty(value) ? "missing" : "accepts any host name";
        }

        if (string.IsNullOrEmpty(value))
        {
            return "missing";
        }

        return rule.Kind switch
        {
            VixlySettingKind.Url => Uri.TryCreate(value, UriKind.Absolute, out Uri? url) && (url.Scheme == Uri.UriSchemeHttps || url.IsLoopback) ? null : "not an absolute https address",
            VixlySettingKind.Email => value.Contains('@', StringComparison.Ordinal) && !value.StartsWith('@') && !value.EndsWith('@') ? null : "not an email address",
            VixlySettingKind.Secret => value.Length >= rule.MinLength ? null : "shorter than " + rule.MinLength + " characters",
            VixlySettingKind.Template => value.Contains("{slug}", StringComparison.Ordinal) ? null : "does not contain {slug}",
            VixlySettingKind.EcPrivateKey => Vixly.Shared.Kernel.Security.VixlyCallerKey.IsValid(value) ? null : "not a P-256 private key (64 hex characters, or base64 PKCS#8)",
            VixlySettingKind.Folder => FolderProblem(value, contentRoot),
            _ => null
        };
    }

    private static string? FolderProblem(string value, string contentRoot)
    {
        string folder;

        try
        {
            folder = Path.GetFullPath(value);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return "not a folder path";
        }

        string site = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRoot)) + Path.DirectorySeparatorChar;

        if ((folder + Path.DirectorySeparatorChar).StartsWith(site, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return "inside the site folder: an update that replaces the site would erase it";
        }

        if (!Directory.Exists(folder))
        {
            return "the folder does not exist";
        }

        try
        {
            string probe = Path.Combine(folder, ".vixly-write-check-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "the site cannot write to the folder";
        }
    }

    private static string SetupPage(VixlySettingsReport report)
    {
        HtmlEncoder e = HtmlEncoder.Default;
        string rows = string.Concat(report.Blocking.Select(problem =>
            "<li><code>" + e.Encode(problem.Rule.Key) + "</code> (environment variable <code>" + e.Encode(problem.Rule.EnvironmentName) + "</code>): " + e.Encode(problem.Problem) + "</li>"));

        return "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"robots\" content=\"noindex\">"
            + "<title>This site is not set up yet</title><style>body{font-family:system-ui,sans-serif;max-width:46rem;margin:3rem auto;padding:0 1rem;color:#0F172A}code{background:#F1F5F9;padding:0 .25rem;border-radius:.25rem}li{margin:.5rem 0}</style></head>"
            + "<body><h1>This site is not set up yet</h1><p>These settings are missing or wrong. Set them in <code>appsettings.Production.json</code> or as environment variables, then restart the site. Values are never shown here.</p><ul>"
            + rows + "</ul></body></html>";
    }
}
