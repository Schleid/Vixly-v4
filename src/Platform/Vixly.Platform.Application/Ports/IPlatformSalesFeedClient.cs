using Vixly.Platform.Application.SalesFeed;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSalesFeedClient.cs
//
// PURPOSE:
// Posts one batch of sale events to TechOS, signed (contract version 1).
//
// RESPONSIBILITIES:
// - Say whether the feed is configured (URL, secret and source set), and if not, which setting is missing or invalid
// - Sign each POST with the time of that attempt and read TechOS's answer
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; settings PlatformSales:TechOs:Url and :Secret come from environment variables
// - Never throws for an HTTP answer or a network failure: the answer says what happened
// =========================================================
public interface IPlatformSalesFeedClient
{
    bool Configured { get; }

    // Null when configured; otherwise url_missing, url_not_https, url_no_path, secret_missing or source_missing.
    string? SettingsProblem { get; }

    Task<PlatformSalesFeedAnswer> SendAsync(string body, CancellationToken cancellationToken);
}
