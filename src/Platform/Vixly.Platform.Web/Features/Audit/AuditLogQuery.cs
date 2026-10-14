namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: AuditLogQuery.cs
//
// PURPOSE:
// The audit log filters as the URL carries them.
//
// RESPONSIBILITIES:
// - Hold the person, record, action, days and page
// - Turn the chosen days into a UTC window
//
// IMPORTANT NOTES:
// - From is the start of its day (UTC); To includes its whole day
// - The URL is the shared state (UI-3)
// =========================================================
public sealed record AuditLogQuery(
    Guid? ActorUserId,
    Guid? TargetId,
    string? ActionCode,
    DateOnly? From,
    DateOnly? To,
    int Page)
{
    public DateTimeOffset? FromUtc => From is DateOnly day ? new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    public DateTimeOffset? ToUtc => To is DateOnly day ? new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    public string ToUrl(int page)
    {
        List<string> parts = new();

        if (ActorUserId is Guid actor)
        {
            parts.Add("actor=" + actor);
        }

        if (TargetId is Guid target)
        {
            parts.Add("target=" + target);
        }

        if (!string.IsNullOrEmpty(ActionCode))
        {
            parts.Add("action=" + Uri.EscapeDataString(ActionCode));
        }

        if (From is DateOnly from)
        {
            parts.Add("from=" + from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (To is DateOnly to)
        {
            parts.Add("to=" + to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (page > 1)
        {
            parts.Add("page=" + page);
        }

        return parts.Count == 0 ? "/audit" : "/audit?" + string.Join("&", parts);
    }
}
