using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Messaging;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformOutboxWorker.cs
//
// PURPOSE:
// Runs the outbox dispatcher in Platform.Web until the Worker
// opens (D-T17, Slice 4).
//
// RESPONSIBILITIES:
// - Every few seconds, dispatch due outbox messages in a fresh
//   scope
// - Log failures without stopping
//
// IMPORTANT NOTES:
// - Safe with several web instances: leasing uses UPDLOCK and
//   READPAST
// - Disabled with PlatformOutbox:Enabled = false (tests)
// - Moves to Vixly.Platform.Worker unchanged in Slice 4
// =========================================================
public sealed class PlatformOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    private readonly PlatformOutboxOptions _options;

    private readonly ILogger<PlatformOutboxWorker> _logger;

    public PlatformOutboxWorker(IServiceScopeFactory scopes, IOptions<PlatformOutboxOptions> options, ILogger<PlatformOutboxWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds)));

        do
        {
            try
            {
                await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
                int handled = await scope.ServiceProvider.GetRequiredService<PlatformOutboxDispatcher>().DispatchAsync(LogFailure, stoppingToken);

                if (handled > 0)
                {
                    _logger.LogInformation("Outbox dispatched {Count} message(s).", handled);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox dispatch failed; it will run again.");
            }
        }
        while (await NextTickAsync(timer, stoppingToken));
    }

    // The id and type find the message in the outbox table; the payload is never logged.
    private void LogFailure(PlatformOutboxFailure failure)
    {
        _logger.LogError(
            failure.Exception,
            "Outbox handler failed for message {MessageId} ({MessageType}): {ErrorCode}, attempt {Attempts}.",
            failure.MessageId,
            failure.Type,
            failure.ErrorCode,
            failure.Attempts);
    }

    // Stopping the host cancels the wait: a normal end, not an error.
    private static async Task<bool> NextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
