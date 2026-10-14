using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.UI.Presenters;

namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: PlatformPresenter.cs
//
// PURPOSE:
// The base of every Platform console presenter.
//
// RESPONSIBILITIES:
// - Hold the immutable State and announce each change
// - Run a use case through PlatformUseCaseScope (fresh data,
//   signed-in context) and turn every outcome into an Error
//   code or none
//
// IMPORTANT NOTES:
// - Denied becomes platform.security.access_denied; an
//   unexpected failure becomes ui.error.generic with the logged
//   reference (S-9)
// - Cancelled (component gone) answers null: there is nobody to
//   tell
// =========================================================
public abstract class PlatformPresenter<TState> : PresenterBase
    where TState : class
{
    private readonly PlatformUseCaseScope _scope;

    protected PlatformPresenter(PlatformUseCaseScope scope, TState initialState, ILogger logger)
        : base(logger)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        State = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    public TState State { get; private set; }

    protected void SetState(TState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        NotifyStateChanged();
    }

    protected async Task<Error?> RunUseCaseAsync(
        Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        Error? error = null;
        PresenterRunOutcome outcome = await RunAsync(async cancellationToken =>
            error = await work(_scope.Begin(), cancellationToken));

        return outcome switch
        {
            PresenterRunOutcome.Completed => error,
            PresenterRunOutcome.Denied => PlatformSecurityErrors.AccessDenied,
            PresenterRunOutcome.Failed => Error.Create("ui.error.generic", ("reference", FailureReference ?? string.Empty)),
            _ => null
        };
    }
}
