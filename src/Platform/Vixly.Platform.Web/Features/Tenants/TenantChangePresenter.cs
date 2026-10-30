using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants.ApprovePlatformTenantChange;
using Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChange;
using Vixly.Platform.Application.UseCases.Tenants.RejectPlatformTenantChange;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantChangePresenter.cs
//
// PURPOSE:
// Shows one company change request and lets staff approve or reject it (B12).
//
// RESPONSIBILITIES:
// - Load the request
// - Approve or reject, asking for a second factor when needed
//
// IMPORTANT NOTES:
// - A refused step-up waits for the dialog, then runs again
// =========================================================
public sealed class TenantChangePresenter : PlatformPresenter<TenantChangeState>
{
    private readonly GetPlatformTenantChangeUseCase _get;

    private readonly ApprovePlatformTenantChangeUseCase _approve;

    private readonly RejectPlatformTenantChangeUseCase _reject;

    private Func<Task<bool>>? _waiting;

    public TenantChangePresenter(
        GetPlatformTenantChangeUseCase get,
        ApprovePlatformTenantChangeUseCase approve,
        RejectPlatformTenantChangeUseCase reject,
        PlatformUseCaseScope scope,
        ILogger<TenantChangePresenter> logger)
        : base(scope, TenantChangeState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _approve = approve ?? throw new ArgumentNullException(nameof(approve));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
    }

    public async Task LoadAsync(Guid requestId)
    {
        GetPlatformTenantChangeResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantChangeRequest { RequestId = requestId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Change = response.Change, CanDecide = response.CanDecide, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public Task<bool> ApproveAsync(Guid requestId, string? note)
    {
        return RunAsync(requestId,
            () => RunUseCaseAsync(async (context, cancellationToken) =>
                (await _approve.ExecuteAsync(context, new ApprovePlatformTenantChangeRequest { RequestId = requestId, Note = note }, cancellationToken)).Error),
            "ui.tenant_changes.approved");
    }

    public Task<bool> RejectAsync(Guid requestId, string? note)
    {
        return RunAsync(requestId,
            () => RunUseCaseAsync(async (context, cancellationToken) =>
                (await _reject.ExecuteAsync(context, new RejectPlatformTenantChangeRequest { RequestId = requestId, Note = note }, cancellationToken)).Error),
            "ui.tenant_changes.rejected");
    }

    public async Task ConfirmStepUpAsync()
    {
        Func<Task<bool>>? waiting = _waiting;
        _waiting = null;

        if (waiting is not null)
        {
            await waiting();
        }
    }

    public void CancelStepUp()
    {
        _waiting = null;
        SetState(State with { StepUpPending = false });
    }

    private async Task<bool> RunAsync(Guid requestId, Func<Task<Error?>> action, string doneKey)
    {
        Error? error = await action();

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            _waiting = () => RunAsync(requestId, action, doneKey);
            SetState(State with { StepUpPending = true, Error = null, DoneKey = null });
            return false;
        }

        await LoadAsync(requestId);
        SetState(State with { StepUpPending = false, Error = error, DoneKey = error is null ? doneKey : null });
        return error is null;
    }
}
