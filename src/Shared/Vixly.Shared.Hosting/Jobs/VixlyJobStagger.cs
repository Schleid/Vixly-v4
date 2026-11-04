using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Vixly.Shared.Hosting.Jobs;

// =========================================================
// FILE: VixlyJobStagger.cs
//
// PURPOSE:
// Performance plan P-5 (owner, 2026-11-04): the background jobs of a site start one after another, not all at once.
//
// RESPONSIBILITIES:
// - Give each job its turn: the first waits the start delay, each next one a few seconds more
// - Leave the first visitor after a start-up the database and Platform to themselves
//
// IMPORTANT NOTES:
// - One per site (singleton); turns are given in the order the jobs ask
// - VixlyJobs:StartDelaySeconds (default 20, 0 to 600) and VixlyJobs:SpacingSeconds (default 5, 0 to 120); 0 and 0 start them all at once
// - A job only waits before its first round; its own interval is unchanged
// =========================================================
public sealed class VixlyJobStagger
{
    public const string SectionName = "VixlyJobs";

    private readonly TimeSpan _first;

    private readonly TimeSpan _spacing;

    private int _next = -1;

    public VixlyJobStagger(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _first = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue(SectionName + ":StartDelaySeconds", 20), 0, 600));
        _spacing = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue(SectionName + ":SpacingSeconds", 5), 0, 120));
    }

    // How long the next job waits; each call takes the next turn.
    public TimeSpan NextDelay() => _first + (_spacing * Interlocked.Increment(ref _next));

    public Task WaitTurnAsync(CancellationToken cancellationToken)
    {
        TimeSpan delay = NextDelay();
        return delay <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(delay, cancellationToken);
    }

    public static IServiceCollection Add(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<VixlyJobStagger>();
        return services;
    }
}
