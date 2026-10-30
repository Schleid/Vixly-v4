using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsOutboxWorker.cs
//
// PURPOSE:
// Delivers outbox messages every few seconds inside Operations.Web (Slice 0, D-F6).
//
// RESPONSIBILITIES:
// - Lease due messages in a system scope
// - Handle each message in its own scope, under its tenant
// - Log failures with id, type, code and attempt (never the payload)
//
// IMPORTANT NOTES:
// - A failed round is logged and retried; stopping the host ends it quietly
// =========================================================
public sealed class OperationsOutboxWorker : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopes;

    private readonly ILogger<OperationsOutboxWorker> _logger;

    public OperationsOutboxWorker(IServiceScopeFactory scopes, ILogger<OperationsOutboxWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);

        do
        {
            try
            {
                IReadOnlyList<Guid> leased;

                await using (AsyncServiceScope lease = _scopes.CreateAsyncScope())
                {
                    lease.ServiceProvider.GetRequiredService<IOperationsTenantScope>().EnterSystem();
                    leased = await lease.ServiceProvider.GetRequiredService<OperationsOutboxDispatcher>().LeaseAsync(stoppingToken);
                }

                foreach (Guid messageId in leased)
                {
                    await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<OperationsOutboxDispatcher>().HandleAsync(messageId, LogFailure, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox round failed; it will run again.");
            }
        }
        while (await NextAsync(timer, stoppingToken));
    }

    private void LogFailure(OperationsOutboxFailure failure)
    {
        _logger.LogError(failure.Exception, "Outbox handler failed for message {MessageId} ({MessageType}): {ErrorCode}, attempt {Attempts}.",
            failure.MessageId, failure.Type, failure.ErrorCode, failure.Attempts);
    }

    private static async Task<bool> NextAsync(PeriodicTimer timer, CancellationToken stoppingToken)
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
