namespace Vixly.Platform.Application.UseCases.Jobs.ListPlatformJobRuns;

// =========================================================
// FILE: ListPlatformJobRunsRequest.cs
//
// PURPOSE:
// The Jobs page: each job's latest run, then the run history, by job and failures.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - An unknown job name is refused
// =========================================================
public sealed class ListPlatformJobRunsRequest
{
    public string? Job { get; init; }

    public bool FailedOnly { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
