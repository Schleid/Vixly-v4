namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformSubscriptionClockOptions.cs
//
// PURPOSE:
// Settings for the subscription clock in Platform.Web.
//
// RESPONSIBILITIES:
// - Switch the clock on or off and set how often it runs
//
// IMPORTANT NOTES:
// - Section PlatformSubscriptionClock; off in tests
// - Hourly is enough: trials and periods end on whole days
// =========================================================
public sealed class PlatformSubscriptionClockOptions
{
    public const string SectionName = "PlatformSubscriptionClock";

    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 60;
}
