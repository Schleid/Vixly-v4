namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformJobSummaryItem.cs
//
// PURPOSE:
// One job on the Jobs page: its latest run and its recent failures.
//
// RESPONSIBILITIES:
// - Carry the values of one row
//
// IMPORTANT NOTES:
// - A job that never ran has no latest run
// =========================================================
public sealed record PlatformJobSummaryItem(
    string Job,
    PlatformJobRunItem? Latest,
    DateTimeOffset? LastSucceededUtc,
    int FailedRecently);
