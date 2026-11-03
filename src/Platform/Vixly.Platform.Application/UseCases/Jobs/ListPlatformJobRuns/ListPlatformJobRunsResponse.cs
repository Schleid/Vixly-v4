using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Jobs.ListPlatformJobRuns;

// =========================================================
// FILE: ListPlatformJobRunsResponse.cs
//
// PURPOSE:
// The Jobs page: each job's latest run, then the run history, by job and failures.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformJobRunsResponse
{
    private ListPlatformJobRunsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformJobSummaryItem> Jobs { get; private init; } = Array.Empty<PlatformJobSummaryItem>();

    public PagedResult<PlatformJobRunItem>? Runs { get; private init; }

    public static ListPlatformJobRunsResponse Success(IReadOnlyList<PlatformJobSummaryItem> jobs, PagedResult<PlatformJobRunItem> runs)
    {
        return new ListPlatformJobRunsResponse { Succeeded = true, Jobs = jobs, Runs = runs };
    }

    public static ListPlatformJobRunsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformJobRunsResponse { Error = error };
    }
}
