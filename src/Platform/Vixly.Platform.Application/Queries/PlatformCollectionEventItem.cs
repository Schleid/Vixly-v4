namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformCollectionEventItem.cs
//
// PURPOSE:
// One line of a collections case's history, with who did it.
//
// RESPONSIBILITIES:
// - Carry the code, arguments, note and actor name for the history table
//
// IMPORTANT NOTES:
// - Codes and ids only; the note is internal
// =========================================================
public sealed record PlatformCollectionEventItem(
    DateTimeOffset OccurredUtc,
    string EventCode,
    IReadOnlyDictionary<string, string> Arguments,
    string? Note,
    string? ActorName);
