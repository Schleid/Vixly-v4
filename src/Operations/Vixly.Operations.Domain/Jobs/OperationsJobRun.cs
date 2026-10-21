using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Jobs;

// =========================================================
// FILE: OperationsJobRun.cs
//
// PURPOSE:
// One run of a background job (outbox, Platform feed, later clocks), as Platform records them (ADR-0007).
//
// RESPONSIBILITIES:
// - Record the job, its start and end, how many items it handled and how many failed
//
// IMPORTANT NOTES:
// - A system entity
// - A failed item is counted, never silently skipped (D-6)
// =========================================================
public sealed class OperationsJobRun : AuditableEntity, IOperationsSystemEntity
{
    public const int JobMaxLength = 100;

    public const int ErrorCodeMaxLength = 100;

    private OperationsJobRun()
    {
        Job = string.Empty;
    }

    private OperationsJobRun(string job, DateTimeOffset startedUtc)
        : base(startedUtc)
    {
        Job = job;
    }

    public string Job { get; private set; }

    public DateTimeOffset? FinishedUtc { get; private set; }

    public int Handled { get; private set; }

    public int Failed { get; private set; }

    public string? ErrorCode { get; private set; }

    // =====================================================
    // METHOD: START
    // =====================================================
    public static OperationsJobRun Start(string job, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(job);
        return new OperationsJobRun(job.Length > JobMaxLength ? job[..JobMaxLength] : job, utcNow);
    }

    // =====================================================
    // METHOD: FINISH
    // =====================================================
    public void Finish(int handled, int failed, string? errorCode, DateTimeOffset utcNow)
    {
        Handled = Math.Max(0, handled);
        Failed = Math.Max(0, failed);
        ErrorCode = errorCode is { Length: > ErrorCodeMaxLength } ? errorCode[..ErrorCodeMaxLength] : errorCode;
        FinishedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
