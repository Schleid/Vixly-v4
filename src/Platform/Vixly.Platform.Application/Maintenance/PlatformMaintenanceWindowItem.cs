using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceWindowItem.cs
//
// PURPOSE:
// One maintenance window, as Control lists it.
//
// RESPONSIBILITIES:
// - Carry the values of one row and its status now
//
// IMPORTANT NOTES:
// - Never carries the pass hash
// =========================================================
public sealed record PlatformMaintenanceWindowItem(
    Guid Id,
    DateTimeOffset StartsUtc,
    DateTimeOffset EndsUtc,
    IReadOnlyList<string> Sites,
    int NoticeHours,
    string? MessageEn,
    string? MessageFr,
    string? MessageEs,
    PlatformMaintenanceStatus Status,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CancelledUtc,
    DateTimeOffset? EndedEarlyUtc)
{
    public static PlatformMaintenanceWindowItem Of(PlatformMaintenanceWindow window, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(window);

        return new PlatformMaintenanceWindowItem(window.Id, window.StartsUtc, window.EndsUtc, PlatformMaintenanceSiteCodes.Of(window.Sites), window.NoticeHours,
            window.MessageEn, window.MessageFr, window.MessageEs, window.StatusAt(utcNow), window.CreatedUtc, window.CancelledUtc, window.EndedEarlyUtc);
    }
}
