namespace Vixly.Platform.Application.UseCases.Maintenance.SchedulePlatformMaintenanceWindow;

// =========================================================
// FILE: SchedulePlatformMaintenanceWindowRequest.cs
//
// PURPOSE:
// Vixly IT publishes a maintenance window: start, end, sites, advance notice and message.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SchedulePlatformMaintenanceWindowRequest
{
    public DateTimeOffset StartsUtc { get; init; }

    public DateTimeOffset EndsUtc { get; init; }

    public IReadOnlyList<string> Sites { get; init; } = Array.Empty<string>();

    public int NoticeHours { get; init; } = 48;

    public string? MessageEn { get; init; }

    public string? MessageFr { get; init; }

    public string? MessageEs { get; init; }
}
