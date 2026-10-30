using Vixly.Operations.Domain.Jobs;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsJobRunRepository.cs
//
// PURPOSE:
// Stores job runs.
//
// RESPONSIBILITIES:
// - Add a run
//
// IMPORTANT NOTES:
// - Runs are kept for review; a later clock removes old ones
// =========================================================
public interface IOperationsJobRunRepository
{
    void Add(OperationsJobRun run);
}
