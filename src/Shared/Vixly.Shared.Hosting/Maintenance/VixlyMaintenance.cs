using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: VixlyMaintenance.cs
//
// PURPOSE:
// Closes a site during a maintenance window Vixly IT published in Control (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Register the monitor
// - During a running window, answer 503 with the maintenance page and Retry-After
// - Let a person with the pass link through until the window ends (a cookie)
//
// IMPORTANT NOTES:
// - Each plane says which site a request belongs to; null leaves it open (health, files, the pass link itself)
// - The pass link is /maintenance-pass/{token} at the root, or under a site's first path segment (/staff/maintenance-pass/{token})
// - The answer during a window carries X-Vixly-Maintenance, so the Cloudflare Worker passes it through (2026-10-28)
// - The cookie holds the window id and end, protected; it opens every site the window closes on that host
// - The page is plain HTML with the stylesheet of the plane: an application under maintenance may be half-deployed
// =========================================================
public static class VixlyMaintenance
{
    public const string PassPath = "/maintenance-pass";

    public const string CookieName = "__Host-vx-maintenance";

    // The Worker in front of the sites lets an answer with this header through (owner, 2026-10-28).
    public const string MarkerHeader = "X-Vixly-Maintenance";

    private const string ProtectorPurpose = "Vixly.Shared.Hosting.Maintenance.Pass";

    public static IServiceCollection AddVixlyMaintenance(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<VixlyMaintenanceOptions>().Bind(configuration.GetSection(VixlyMaintenanceOptions.SectionName));
        services.AddSingleton<VixlyMaintenanceMonitor>();
        return services;
    }

    // Paths a closed site still serves: health, its files, and the pass link.
    public static bool IsAlwaysOpen(PathString path)
    {
        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/img", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase);
    }

    public static IApplicationBuilder UseVixlyMaintenance(
        this IApplicationBuilder app,
        Func<HttpContext, string?> site,
        Func<HttpContext, VixlyMaintenanceWindow, VixlyMaintenancePageText> text,
        string stylesheet)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(text);

        return app.Use(async (http, next) =>
        {
            // A status page the pipeline runs again for this request (a 404 page) is not closed twice.
            if (site(http) is not { } name || http.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>() is not null)
            {
                await next(http);
                return;
            }

            VixlyMaintenanceMonitor monitor = http.RequestServices.GetRequiredService<VixlyMaintenanceMonitor>();

            if (PassLink(http.Request.Path) is { } link)
            {
                await PassAsync(http, monitor, name, link.Token, link.Home);
                return;
            }

            if (monitor.Running(name) is not { } window || HasPass(http, window, monitor.UtcNow))
            {
                await next(http);
                return;
            }

            // Placed before the plane's own language step, the page follows the browser's language.
            if (http.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>() is null)
            {
                System.Globalization.CultureInfo culture = new(Language(http.Request.Headers.AcceptLanguage.ToString()));
                System.Globalization.CultureInfo.CurrentCulture = culture;
                System.Globalization.CultureInfo.CurrentUICulture = culture;
            }

            VixlyMaintenancePageText words = text(http, window);
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            http.Response.Headers.RetryAfter = Math.Max(60, (int)Math.Ceiling((window.EndsUtc - monitor.UtcNow).TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            http.Response.Headers.CacheControl = "no-store";
            http.Response.Headers[MarkerHeader] = "1";
            http.Response.ContentType = "text/html; charset=utf-8";
            await http.Response.WriteAsync(Html(words, http.Request.PathBase + stylesheet));
        });
    }

    public static string Html(VixlyMaintenancePageText text, string stylesheet)
    {
        ArgumentNullException.ThrowIfNull(text);

        HtmlEncoder e = HtmlEncoder.Default;
        string message = string.IsNullOrWhiteSpace(text.Message) ? string.Empty : "<p>" + e.Encode(text.Message) + "</p>";

        return "<!DOCTYPE html><html lang=\"" + e.Encode(text.Language) + "\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<meta name=\"robots\" content=\"noindex\"><title>" + e.Encode(text.Title) + "</title><link rel=\"stylesheet\" href=\"" + e.Encode(stylesheet) + "\"></head>"
            + "<body><main class=\"vx-error vx-maintenance\"><h1>" + e.Encode(text.Title) + "</h1><p>" + e.Encode(text.Body) + "</p>" + message + "</main></body></html>";
    }

    // en, fr or es: the first the browser asks for, English otherwise.
    public static string Language(string? acceptLanguage)
    {
        foreach (string part in (acceptLanguage ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string code = part.Split(';')[0].Split('-')[0].Trim().ToLowerInvariant();

            if (code is "en" or "fr" or "es")
            {
                return code;
            }
        }

        return "en";
    }

    // The token and the page to go to after: /maintenance-pass/{token} goes home; /staff/maintenance-pass/{token} goes to /staff/.
    public static (string Token, string Home)? PassLink(PathString path)
    {
        if (path.StartsWithSegments(PassPath, StringComparison.OrdinalIgnoreCase, out PathString rest))
        {
            return (rest.Value?.Trim('/') ?? string.Empty, "/");
        }

        string value = path.Value ?? string.Empty;
        int second = value.Length > 1 ? value.IndexOf('/', 1) : -1;

        // Letters, digits and dashes only: the segment becomes the redirect, so "/\\host" can never be one.
        if (second > 1 && value[1..second].All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            && new PathString(value[second..]).StartsWithSegments(PassPath, StringComparison.OrdinalIgnoreCase, out PathString after))
        {
            return (after.Value?.Trim('/') ?? string.Empty, value[..second] + "/");
        }

        return null;
    }

    private static async Task PassAsync(HttpContext http, VixlyMaintenanceMonitor monitor, string site, string token, string home)
    {
        http.Response.Headers.CacheControl = "no-store";

        if (token.Length is 0 or > 200 || await monitor.CheckPassAsync(site, token, http.RequestAborted) is not { } endsUtc)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // A window just published may not be in this site's copy yet.
        await monitor.RefreshAsync(site, http.RequestAborted);

        if ((monitor.Running(site) ?? monitor.Announced(site)) is not { } window)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string value = Protector(http).Protect(window.Id.ToString("N") + "|" + endsUtc.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        http.Response.Cookies.Append(CookieName, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = endsUtc
        });
        http.Response.Redirect(http.Request.PathBase + home);
    }

    private static bool HasPass(HttpContext http, VixlyMaintenanceWindow window, DateTimeOffset utcNow)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName, out string? value) || string.IsNullOrEmpty(value))
        {
            return false;
        }

        try
        {
            string[] parts = Protector(http).Unprotect(value).Split('|');
            return parts.Length == 2
                && parts[0] == window.Id.ToString("N")
                && long.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long ticks)
                && new DateTimeOffset(ticks, TimeSpan.Zero) > utcNow;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }

    private static IDataProtector Protector(HttpContext http)
    {
        return http.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(ProtectorPurpose);
    }
}
