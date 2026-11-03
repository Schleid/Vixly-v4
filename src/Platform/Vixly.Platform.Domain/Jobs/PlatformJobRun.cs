using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Jobs;

// =========================================================
// FILE: PlatformJobRun.cs
//
// PURPOSE:
// One run of a Control background job (ADR-0007): the subscription clock's steps, as Operations records them in ops.JobRuns.
//
// RESPONSIBILITIES:
// - Record the job, its start and end, how many items it handled, whether it failed and why
//
// IMPORTANT NOTES:
// - A failed step is recorded, never silently skipped (D-6)
// - The error is the exception's type name, never its message (it may carry data)
// - Kept 90 days
// =========================================================
public sealed class PlatformJobRun : AuditableEntity
{
    public const int JobMaxLength = 100;

    public const int ErrorCodeMaxLength = 200;

    public static readonly TimeSpan Retention = TimeSpan.FromDays(90);

    private PlatformJobRun()
    {
        Job = string.Empty;
    }

    private PlatformJobRun(string job, DateTimeOffset startedUtc)
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
    public static PlatformJobRun Start(string job, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(job);

        return new PlatformJobRun(job.Length > JobMaxLength ? job[..JobMaxLength] : job, utcNow);
    }

    // =====================================================
    // METHOD: FINISH
    // =====================================================
    public void Finish(int handled, int failed, string? errorCode, DateTimeOffset utcNow)
    {
        Handled = Math.Max(0, handled);
        Failed = Math.Max(0, failed);
        ErrorCode = errorCode is { Length: > ErrorCodeMaxLength } ? errorCode[..ErrorCodeMaxLength] : errorCode;
        FinishedUtc = utcNow < CreatedUtc ? CreatedUtc : utcNow;
        MarkAsModified(FinishedUtc.Value);
    }
}
