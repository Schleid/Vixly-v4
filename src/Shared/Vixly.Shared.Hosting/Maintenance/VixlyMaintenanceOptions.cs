namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: VixlyMaintenanceOptions.cs
//
// PURPOSE:
// Settings of the maintenance check on a site.
//
// RESPONSIBILITIES:
// - Turn the check on or off and set how often it asks
//
// IMPORTANT NOTES:
// - Section VixlyMaintenance; on by default
// - Every minute by default
// =========================================================
public sealed class VixlyMaintenanceOptions
{
    public const string SectionName = "VixlyMaintenance";

    public bool Enabled { get; set; } = true;

    public int RefreshSeconds { get; set; } = 60;
}
