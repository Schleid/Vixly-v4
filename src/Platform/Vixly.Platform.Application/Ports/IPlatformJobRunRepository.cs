using Vixly.Platform.Domain.Jobs;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformJobRunRepository.cs
//
// PURPOSE:
// Stores the runs of Control's background jobs (ADR-0007).
//
// RESPONSIBILITIES:
// - Add a run
// - Remove runs past their retention
//
// IMPORTANT NOTES:
// - Append only
// =========================================================
public interface IPlatformJobRunRepository
{
    void Add(PlatformJobRun run);

    Task<int> DeleteCreatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
}
