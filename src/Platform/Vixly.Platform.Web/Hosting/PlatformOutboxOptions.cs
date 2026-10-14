namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformOutboxOptions.cs
//
// PURPOSE:
// Configuration of the outbox dispatcher (section PlatformOutbox).
//
// RESPONSIBILITIES:
// - Turn dispatching on or off and set its interval
//
// IMPORTANT NOTES:
// - On by default; tests turn it off
// =========================================================
public sealed class PlatformOutboxOptions
{
    public const string SectionName = "PlatformOutbox";

    public bool Enabled { get; set; } = true;

    public int IntervalSeconds { get; set; } = 10;
}
