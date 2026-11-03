using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyHealthDetail.cs
//
// PURPOSE:
// Writes /health/ready, and tells the owner why a site is degraded or unhealthy (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Without the key: only the status word, as before
// - With the key (?key= or the X-Vixly-Health-Key header): one line per check, its status,
//   what is wrong and, for a failure, the error's type and message
//
// IMPORTANT NOTES:
// - The key is VixlyHealth:DetailKey (environment: VixlyHealth__DetailKey), 32 characters or more;
//   unset or shorter, details are off
// - Compared in constant time; the answer is never cached
// - Names settings, never their values; error messages can name a server, hence the key
// =========================================================
public static class VixlyHealthDetail
{
    public const string KeySetting = "VixlyHealth:DetailKey";

    public const string KeyHeader = "X-Vixly-Health-Key";

    private const int MinimumKeyLength = 32;

    public static Task WriteAsync(HttpContext http, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(report);

        http.Response.Headers.CacheControl = "no-store";
        http.Response.ContentType = "text/plain; charset=utf-8";
        if (!Allowed(http))
        {
            return http.Response.WriteAsync(report.Status.ToString());
        }

        StringBuilder text = new();
        text.Append(report.Status).Append(" (").Append((int)report.TotalDuration.TotalMilliseconds).AppendLine(" ms)");
        foreach ((string name, HealthReportEntry entry) in report.Entries.OrderBy(item => item.Value.Status).ThenBy(item => item.Key, StringComparer.Ordinal))
        {
            text.AppendLine();
            text.Append(name).Append(": ").Append(entry.Status).Append(" (").Append((int)entry.Duration.TotalMilliseconds).AppendLine(" ms)");
            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                text.Append("  ").AppendLine(entry.Description);
            }

            if (entry.Exception is { } error)
            {
                text.Append("  ").Append(error.GetType().Name).Append(": ").AppendLine(FirstLine(error.GetBaseException().Message));
            }
        }

        return http.Response.WriteAsync(text.ToString());
    }

    public static bool Allowed(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        string? expected = http.RequestServices.GetService<IConfiguration>()?[KeySetting]?.Trim();
        if (expected is null || expected.Length < MinimumKeyLength)
        {
            return false;
        }

        string? given = http.Request.Headers[KeyHeader].FirstOrDefault() ?? http.Request.Query["key"].FirstOrDefault();
        return given is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given.Trim()), Encoding.UTF8.GetBytes(expected));
    }

    private static string FirstLine(string message)
    {
        int end = message.IndexOfAny(new[] { '\r', '\n' });
        return end < 0 ? message : message[..end];
    }
}
