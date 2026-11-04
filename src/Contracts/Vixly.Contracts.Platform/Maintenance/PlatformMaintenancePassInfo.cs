namespace Vixly.Contracts.Platform.Maintenance;

// =========================================================
// FILE: PlatformMaintenancePassInfo.cs
//
// PURPOSE:
// A pass link Platform accepted: good until the window ends.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and a site
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformMaintenancePassInfo
{
    public DateTimeOffset EndsUtc { get; init; }
}
