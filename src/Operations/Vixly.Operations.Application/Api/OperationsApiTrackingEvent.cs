namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiTrackingEvent.cs
//
// PURPOSE:
// A tracking event as the company API shows it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry what the company API and its webhooks show
//
// IMPORTANT NOTES:
// - Ids, codes and figures only: never a password, a protected value or staff notes
// - A field is only ever added, never renamed or removed (receivers depend on it)
// =========================================================
public sealed record OperationsApiTrackingEvent(
    Guid Id,
    string Subject,
    Guid SubjectId,
    string Code,
    IReadOnlyDictionary<string, string> Arguments,
    DateTimeOffset OccurredUtc,
    string? TimeZone,
    string? LocationCode,
    string Visibility,
    Guid? VoidsEventId,
    DateTimeOffset CreatedUtc);
