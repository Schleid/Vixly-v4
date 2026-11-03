namespace Vixly.Platform.Domain.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceStatus.cs
//
// PURPOSE:
// Where a maintenance window stands at a given time.
//
// RESPONSIBILITIES:
// - Name each stage
//
// IMPORTANT NOTES:
// - Worked out from the times; never stored
// =========================================================
public enum PlatformMaintenanceStatus
{
    Scheduled = 0,
    Announced = 1,
    Running = 2,
    Finished = 3,
    Cancelled = 4
}
