namespace Vixly.Shared.Hosting.Diagnostics;

// =========================================================
// FILE: VixlyRequestTimingOptions.cs
//
// PURPOSE:
// Settings of the request timing (performance plan P-1, owner 2026-11-04).
//
// RESPONSIBILITIES:
// - Say from how many milliseconds a request is logged as slow, and whether the timing header may be shown
//
// IMPORTANT NOTES:
// - VixlyTiming:SlowRequestMs (default 1000; 0 logs none); VixlyTiming:ShowHeader (default false)
// - Not secrets: appsettings.json or environment variables
// =========================================================
public sealed class VixlyRequestTimingOptions
{
    public const string SectionName = "VixlyTiming";

    public int SlowRequestMs { get; set; } = 1000;

    public bool ShowHeader { get; set; }
}
