using Vixly.Platform.Application.Jobs;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Jobs.ListPlatformJobRuns;

// =========================================================
// FILE: ListPlatformJobRunsUseCase.cs
//
// PURPOSE:
// The Jobs page: each job's latest run, then the run history, by job and failures.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then read
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.jobs.view
// - Every known job is listed, even one that never ran
// - Failures counted over the last 24 hours
// =========================================================
public sealed class ListPlatformJobRunsUseCase
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromHours(24);

    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformJobQueries _queries;

    private readonly TimeProvider _timeProvider;

    public ListPlatformJobRunsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformJobQueries queries,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListPlatformJobRunsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformJobRunsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.JobsView, cancellationToken);

        string? job = string.IsNullOrWhiteSpace(request.Job) ? null : request.Job.Trim();

        if (job is not null && !PlatformJobNames.All.Contains(job, StringComparer.Ordinal))
        {
            return ListPlatformJobRunsResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        IReadOnlyList<PlatformJobSummaryItem> found = await _queries.ListLatestAsync(_timeProvider.GetUtcNow() - FailureWindow, cancellationToken);
        IReadOnlyList<PlatformJobSummaryItem> jobs = PlatformJobNames.All
            .Select(name => found.FirstOrDefault(item => item.Job == name) ?? new PlatformJobSummaryItem(name, null, null, 0))
            .ToList();

        return ListPlatformJobRunsResponse.Success(
            jobs,
            await _queries.ListRunsAsync(job, request.FailedOnly, PagedRequest.Create(request.Page, request.PageSize), cancellationToken));
    }
}
