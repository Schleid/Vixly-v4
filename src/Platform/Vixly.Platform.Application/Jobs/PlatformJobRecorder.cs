using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Jobs;

namespace Vixly.Platform.Application.Jobs;

// =========================================================
// FILE: PlatformJobRecorder.cs
//
// PURPOSE:
// Writes one run of a background job (ADR-0007).
//
// RESPONSIBILITIES:
// - Save the job, its start and end, what it handled and any failure
//
// IMPORTANT NOTES:
// - Used in its own scope, after the step, so a failed step never stops its record
// =========================================================
public sealed class PlatformJobRecorder
{
    private readonly IPlatformJobRunRepository _runs;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformJobRecorder(IPlatformJobRunRepository runs, IPlatformUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task RecordAsync(string job, DateTimeOffset startedUtc, int handled, string? errorCode, CancellationToken cancellationToken)
    {
        PlatformJobRun run = PlatformJobRun.Start(job, startedUtc);
        run.Finish(handled, errorCode is null ? 0 : 1, errorCode, _timeProvider.GetUtcNow());
        _runs.Add(run);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
