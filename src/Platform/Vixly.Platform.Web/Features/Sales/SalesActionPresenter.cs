using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: SalesActionPresenter.cs
//
// PURPOSE:
// Shared plumbing for presenters that run one action at a time with step-up.
//
// RESPONSIBILITIES:
// - Run an action, turn "step-up required" into the dialog, keep the done message and a one-time link
//
// IMPORTANT NOTES:
// - Abstract: never registered itself
// - The step-up dialog calls the same action again once confirmed
// =========================================================
public abstract class SalesActionPresenter : PlatformPresenter<SalesActionState>
{
    protected SalesActionPresenter(PlatformUseCaseScope scope, ILogger logger)
        : base(scope, SalesActionState.Initial, logger)
    {
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public void Clear()
    {
        SetState(SalesActionState.Initial);
    }

    protected async Task<bool> ActAsync(string doneKey, Func<PlatformAuthorizationContext, CancellationToken, Task<(Error? Error, string? Link)>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        string? link = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            (Error? failed, string? issued) = await work(context, cancellationToken);
            link = issued;
            return failed;
        });

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(new SalesActionState(false, error is null ? doneKey : null, error, error is null ? link : null));

        return error is null;
    }
}
