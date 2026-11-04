using System.Diagnostics;

namespace Vixly.Shared.Hosting.Diagnostics;

// =========================================================
// FILE: VixlyRequestTimes.cs
//
// PURPOSE:
// What one request spent in the database and in calls to other sites (performance plan P-1).
//
// RESPONSIBILITIES:
// - Count SQL commands and outgoing calls, and add up their time
//
// IMPORTANT NOTES:
// - One per request, kept in the request's features; safe when commands run in parallel
// - Belongs to one site's listener, so two sites in one process (tests) never count each other's commands
// =========================================================
public sealed class VixlyRequestTimes
{
    private long _sqlCount;

    private long _sqlTicks;

    private long _callCount;

    private long _callTicks;

    public VixlyRequestTimes(object owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    // The site's own listener; another site in the same process never adds to this request.
    public object Owner { get; }

    public long Started { get; } = Stopwatch.GetTimestamp();

    public long SqlCount => Interlocked.Read(ref _sqlCount);

    public double SqlMs => Stopwatch.GetElapsedTime(0, Interlocked.Read(ref _sqlTicks)).TotalMilliseconds;

    public long CallCount => Interlocked.Read(ref _callCount);

    public double CallMs => Stopwatch.GetElapsedTime(0, Interlocked.Read(ref _callTicks)).TotalMilliseconds;

    public double TotalMs => Stopwatch.GetElapsedTime(Started).TotalMilliseconds;

    public void AddSql(TimeSpan duration)
    {
        Interlocked.Increment(ref _sqlCount);
        Interlocked.Add(ref _sqlTicks, (long)(duration.TotalSeconds * Stopwatch.Frequency));
    }

    public void AddCall(TimeSpan duration)
    {
        Interlocked.Increment(ref _callCount);
        Interlocked.Add(ref _callTicks, (long)(duration.TotalSeconds * Stopwatch.Frequency));
    }

    // Server-Timing: sql and calls with their counts, then the whole request so far.
    public string Header() => FormattableString.Invariant(
        $"sql;dur={SqlMs:F1};desc=\"{SqlCount}\", calls;dur={CallMs:F1};desc=\"{CallCount}\", app;dur={TotalMs:F1}");
}
