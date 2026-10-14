namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoDataOptions.cs
//
// PURPOSE:
// Settings for loading demo data at Development start-up.
//
// RESPONSIBILITIES:
// - Switch start-up seeding on or off
//
// IMPORTANT NOTES:
// - Section PlatformDemoData; read only in Development
// =========================================================
public sealed class PlatformDemoDataOptions
{
    public const string SectionName = "PlatformDemoData";

    public bool SeedOnStartup { get; set; } = true;
}
