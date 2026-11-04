namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformJobRunItem.cs
//
// PURPOSE:
// One run of a background job, as the Jobs page shows it.
//
// RESPONSIBILITIES:
// - Carry the values of one row
//
// IMPORTANT NOTES:
// - Finished is empty while the run is going (or when the host stopped during it)
// =========================================================
public sealed record PlatformJobRunItem(
    Guid Id,
    string Job,
    DateTimeOffset StartedUtc,
    DateTimeOffset? FinishedUtc,
    int Handled,
    int Failed,
    string? ErrorCode);
