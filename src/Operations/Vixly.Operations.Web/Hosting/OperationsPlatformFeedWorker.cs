using Vixly.Operations.Application.Platform;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsPlatformFeedWorker.cs
//
// PURPOSE:
// Reads the Platform event feed every minute inside Operations.Web (Slice 0, D-F3, D-F6).
//
// RESPONSIBILITIES:
// - Run the feed clock once a minute in its own scope
//
// IMPORTANT NOTES:
// - Off when no Platform address is configured
// =========================================================
public sealed class OperationsPlatformFeedWorker : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;

    private readonly IConfiguration _configuration;

    private readonly ILogger<OperationsPlatformFeedWorker> _logger;

    public OperationsPlatformFeedWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<OperationsPlatformFeedWorker> logger)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration["OperationsPlatform:CallerPrivateKey"]))
        {
            _logger.LogWarning("No Platform caller key is set: the Platform event feed is not read.");
            return;
        }

        using PeriodicTimer timer = new(Interval);

        do
        {
            try
            {
                await using AsyncServiceScope scope = _scopes.CreateAsyncScope();
                int handled = await scope.ServiceProvider.GetRequiredService<OperationsPlatformFeedClock>().RunAsync(stoppingToken);

                if (handled > 0)
                {
                    _logger.LogInformation("Platform feed: {Count} event(s) read.", handled);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Platform feed round failed; it will run again.");
            }
        }
        while (await NextAsync(timer, stoppingToken));
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
