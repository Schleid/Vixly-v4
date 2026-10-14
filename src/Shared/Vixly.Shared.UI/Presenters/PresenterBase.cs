using Microsoft.Extensions.Logging;

namespace Vixly.Shared.UI.Presenters;

// =========================================================
// FILE: PresenterBase.cs
//
// PURPOSE:
// The plane-agnostic base of every presenter (ADR-0001, UI-4).
//
// RESPONSIBILITIES:
// - Run one operation at a time with a busy flag
// - Tie cancellation to the owning component's lifetime
// - Turn an authorization refusal into Denied and any other
//   exception into Failed with a logged reference (S-9)
// - Raise StateChanged so the component re-renders
//
// IMPORTANT NOTES:
// - No Blazor types: presenters are unit-tested without a host
//   (UI-9)
// - A failure is logged with its reference and never shown raw;
//   only the reference reaches the user (S-9, D-6)
// - Owned by the Frontend/UI team (Shared.UI); review with them
// =========================================================
public abstract class PresenterBase : IDisposable
{
    private static readonly Action<ILogger, string, Exception?> LogFailure =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "PresenterFailed"), "Presenter operation failed. Reference {Reference}");

    private static readonly Action<ILogger, Exception?> LogDenied =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, "PresenterDenied"), "Presenter operation was denied by an authorization gate");

    private readonly CancellationTokenSource _lifetime = new();

    private readonly ILogger _logger;

    private int _running;

    private bool _disposed;

    protected PresenterBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event Action? StateChanged;

    public bool IsBusy => _running > 0;

    public string? FailureReference { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void NotifyStateChanged()
    {
        if (!_disposed)
        {
            StateChanged?.Invoke();
        }
    }

    protected async Task<PresenterRunOutcome> RunAsync(Func<CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (_disposed)
        {
            return PresenterRunOutcome.Cancelled;
        }

        _running++;
        FailureReference = null;
        NotifyStateChanged();

        try
        {
            await work(_lifetime.Token);
            return PresenterRunOutcome.Completed;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // The component is gone; there is nobody to tell.
            return PresenterRunOutcome.Cancelled;
        }
        catch (UnauthorizedAccessException exception)
        {
            LogDenied(_logger, exception);
            return PresenterRunOutcome.Denied;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            FailureReference = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            LogFailure(_logger, FailureReference, exception);
            return PresenterRunOutcome.Failed;
        }
        finally
        {
            _running--;
            NotifyStateChanged();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            StateChanged = null;
        }
    }
}
