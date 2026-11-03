using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Jobs.ListPlatformJobRuns;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Jobs;

// =========================================================
// FILE: JobRunsPresenter.cs
//
// PURPOSE:
// Loads the Jobs page: each background job's latest run and the run history (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - Run ListPlatformJobRuns for one job or all, one page at a time
//
// IMPORTANT NOTES:
// - Presenter (ADR-0001): no markup
// =========================================================
public sealed class JobRunsPresenter : PlatformPresenter<JobRunsState>
{
    private readonly ListPlatformJobRunsUseCase _list;

    public JobRunsPresenter(
        ListPlatformJobRunsUseCase list,
        PlatformUseCaseScope scope,
        ILogger<JobRunsPresenter> logger)
        : base(scope, JobRunsState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(string? job, bool failedOnly, int page)
    {
        ListPlatformJobRunsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformJobRunsRequest { Job = job, FailedOnly = failedOnly, Page = Math.Max(page, 1) }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Runs is { } runs
            ? new JobRunsState(true, response.Jobs, runs.Items, runs.Page, runs.TotalPages, runs.TotalCount, null)
            : JobRunsState.Initial with { Loaded = true, Error = error });
    }
}
