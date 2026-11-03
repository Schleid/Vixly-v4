namespace Vixly.Platform.Infrastructure.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedOptions.cs
//
// PURPOSE:
// Where Vixly Control sends its sale events for TechOS (contract version 1, 2026-11-02).
//
// RESPONSIBILITIES:
// - The TechOS receiving address, the shared secret, the source name and the timings
//
// IMPORTANT NOTES:
// - Url and Secret are environment variables (PlatformSales__TechOs__Url, PlatformSales__TechOs__Secret), never in a settings file
// - Missing Url or Secret: the feed is off, but events are still written and wait
// - Only https addresses are used
// =========================================================
public sealed class PlatformSalesFeedOptions
{
    public const string SectionName = "PlatformSales:TechOs";

    public string Url { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    // The X-TC-Source header; TechOS checks it against its path key.
    public string Source { get; set; } = string.Empty;

    public int IntervalSeconds { get; set; } = 60;

    public int TimeoutSeconds { get; set; } = 30;

    public bool Enabled { get; set; } = true;
}
