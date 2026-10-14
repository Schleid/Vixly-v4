namespace Vixly.Platform.Infrastructure.Hosting;

// =========================================================
// FILE: PlatformHostingOptions.cs
//
// PURPOSE:
// Where the Platform console is reachable, for links in
// emails (Global Standard H-1).
//
// RESPONSIBILITIES:
// - Hold the console base URL from configuration
//   (PlatformHosting:ConsoleBaseUrl)
//
// IMPORTANT NOTES:
// - Never a literal in code: vixly-control.techcrafters.ltd
//   until migration, control.vixly.ltd after
// =========================================================
public sealed class PlatformHostingOptions
{
    public const string SectionName = "PlatformHosting";

    public string ConsoleBaseUrl { get; set; } = string.Empty;
}
