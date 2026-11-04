using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Jobs;

// =========================================================
// FILE: JobRunsState.cs
//
// PURPOSE:
// What the Jobs page shows (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - Hold each job's latest run, one page of runs, the paging numbers and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record JobRunsState(
    bool Loaded,
    IReadOnlyList<PlatformJobSummaryItem> Jobs,
    IReadOnlyList<PlatformJobRunItem> Runs,
    int Page,
    int TotalPages,
    int TotalCount,
    Error? Error)
{
    public static JobRunsState Initial { get; } = new(false, Array.Empty<PlatformJobSummaryItem>(), Array.Empty<PlatformJobRunItem>(), 1, 0, 0, null);
}
