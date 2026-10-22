namespace Vixly.Shared.Kernel.Clients;

// =========================================================
// FILE: VixlyUserAgents.cs
//
// PURPOSE:
// The browser and operating system a person uses, named from the
// browser's user agent, for device and activity lists (owner,
// 2026-10-21).
//
// RESPONSIBILITIES:
// - Name the common browsers and systems; anything else is none
//
// IMPORTANT NOTES:
// - A hint for people reading a list, never a security check: a
//   browser can say anything
// - Order matters: Edge and Opera also say Chrome; Chrome also
//   says Safari
// =========================================================
public static class VixlyUserAgents
{
    public static string? Browser(string? userAgent)
    {
        string text = userAgent ?? string.Empty;

        return text switch
        {
            _ when text.Contains("Edg/", StringComparison.Ordinal) || text.Contains("EdgA/", StringComparison.Ordinal) || text.Contains("EdgiOS/", StringComparison.Ordinal) => "Edge",
            _ when text.Contains("OPR/", StringComparison.Ordinal) => "Opera",
            _ when text.Contains("SamsungBrowser/", StringComparison.Ordinal) => "Samsung Internet",
            _ when text.Contains("Firefox/", StringComparison.Ordinal) || text.Contains("FxiOS/", StringComparison.Ordinal) => "Firefox",
            _ when text.Contains("Chrome/", StringComparison.Ordinal) || text.Contains("CriOS/", StringComparison.Ordinal) => "Chrome",
            _ when text.Contains("Safari/", StringComparison.Ordinal) && text.Contains("Version/", StringComparison.Ordinal) => "Safari",
            _ => null
        };
    }

    public static string? System(string? userAgent)
    {
        string text = userAgent ?? string.Empty;

        return text switch
        {
            _ when text.Contains("Windows", StringComparison.Ordinal) => "Windows",
            _ when text.Contains("iPhone", StringComparison.Ordinal) || text.Contains("iPad", StringComparison.Ordinal) => "iOS",
            _ when text.Contains("Android", StringComparison.Ordinal) => "Android",
            _ when text.Contains("CrOS", StringComparison.Ordinal) => "ChromeOS",
            _ when text.Contains("Mac OS X", StringComparison.Ordinal) || text.Contains("Macintosh", StringComparison.Ordinal) => "macOS",
            _ when text.Contains("Linux", StringComparison.Ordinal) => "Linux",
            _ => null
        };
    }
}
