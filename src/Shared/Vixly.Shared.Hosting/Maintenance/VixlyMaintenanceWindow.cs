namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: VixlyMaintenanceWindow.cs
//
// PURPOSE:
// A maintenance window as a site sees it: announced (a banner) or running (the maintenance page).
//
// RESPONSIBILITIES:
// - Carry the times and the message in en, fr and es
// - Pick the message for the visitor's language
//
// IMPORTANT NOTES:
// - Filled by each plane from Platform's answer
// - Times are UTC
// =========================================================
public sealed record VixlyMaintenanceWindow(
    Guid Id,
    DateTimeOffset StartsUtc,
    DateTimeOffset EndsUtc,
    string? MessageEn,
    string? MessageFr,
    string? MessageEs)
{
    public bool IsRunning(DateTimeOffset utcNow) => utcNow >= StartsUtc && utcNow < EndsUtc;

    public bool IsAnnounced(DateTimeOffset utcNow) => utcNow < StartsUtc;

    public string? Message(string? language) => language switch
    {
        "fr" => MessageFr ?? MessageEn,
        "es" => MessageEs ?? MessageEn,
        _ => MessageEn
    };
}
