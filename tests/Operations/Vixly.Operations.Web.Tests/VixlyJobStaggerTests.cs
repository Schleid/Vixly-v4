using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vixly.Shared.Hosting.Jobs;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: VixlyJobStaggerTests.cs
//
// PURPOSE:
// Proves the staggered start of the background jobs (performance plan P-5, owner 2026-11-04).
//
// RESPONSIBILITIES:
// - Each job gets the next turn: 20 seconds, then 5 more each, unless the settings say otherwise
// - The settings are kept within their bounds; 0 and 0 start every job at once
// - Every background job of the site can still be built from the site's services
//
// IMPORTANT NOTES:
// - The test host removes the running jobs; they are built here, not run
// =========================================================
public sealed class VixlyJobStaggerTests
{
    [Fact]
    public async Task EachJob_GetsTheNextTurn_WithinTheSettingsBounds()
    {
        VixlyJobStagger standard = new(Settings());
        Assert.Equal(TimeSpan.FromSeconds(20), standard.NextDelay());
        Assert.Equal(TimeSpan.FromSeconds(25), standard.NextDelay());
        Assert.Equal(TimeSpan.FromSeconds(30), standard.NextDelay());

        VixlyJobStagger none = new(Settings(("VixlyJobs:StartDelaySeconds", "0"), ("VixlyJobs:SpacingSeconds", "0")));
        Assert.True(none.WaitTurnAsync(CancellationToken.None).IsCompletedSuccessfully);
        Assert.True(none.WaitTurnAsync(CancellationToken.None).IsCompletedSuccessfully);

        VixlyJobStagger bounded = new(Settings(("VixlyJobs:StartDelaySeconds", "5000"), ("VixlyJobs:SpacingSeconds", "-3")));
        Assert.Equal(TimeSpan.FromSeconds(600), bounded.NextDelay());
        Assert.Equal(TimeSpan.FromSeconds(600), bounded.NextDelay());

        // Stopping the site during the wait ends the job quietly.
        using CancellationTokenSource stop = new();
        Task waiting = new VixlyJobStagger(Settings()).WaitTurnAsync(stop.Token);
        await stop.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }

    [SqlServerFact]
    public async Task EveryBackgroundJob_CanBeBuilt_FromTheSitesServices()
    {
        await using OperationsWebTestHost host = await OperationsWebTestHost.StartAsync();
        Type[] jobs = typeof(Vixly.Operations.Web.Hosting.OperationsOutboxWorker).Assembly.GetTypes()
            .Where(type => typeof(BackgroundService).IsAssignableFrom(type) && !type.IsAbstract)
            .ToArray();

        Assert.True(jobs.Length >= 10);

        foreach (Type job in jobs)
        {
            Assert.NotNull(ActivatorUtilities.CreateInstance(host.Services, job));
        }

        Assert.NotNull(host.Services.GetService<VixlyJobStagger>());
    }

    private static IConfiguration Settings(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(item => new KeyValuePair<string, string?>(item.Key, item.Value))).Build();
}
