namespace Vixly.Contracts.Platform.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceWindowInfo.cs
//
// PURPOSE:
// One maintenance window a site must show: announced (a banner) or running (the maintenance page).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and the sites
//
// IMPORTANT NOTES:
// - Times are UTC; a message per language, each optional
// - Never carries the pass link
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformMaintenanceWindowInfo
{
    public Guid Id { get; init; }

    public DateTimeOffset StartsUtc { get; init; }

    public DateTimeOffset EndsUtc { get; init; }

    public bool Running { get; init; }

    public string? MessageEn { get; init; }

    public string? MessageFr { get; init; }

    public string? MessageEs { get; init; }
}
