using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments.ConfirmPlatformZelleClaim;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleClaim;
using Vixly.Platform.Application.UseCases.Payments.RejectPlatformZelleClaim;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleClaimPresenter.cs
//
// PURPOSE:
// Behaviour of the Zelle report page.
//
// RESPONSIBILITIES:
// - Load the report
// - Confirm (records a Zelle payment) or reject (the customer is told why)
//
// IMPORTANT NOTES:
// - Both ask for step-up; the waiting action runs again once step-up is done
// =========================================================
public sealed class ZelleClaimPresenter : PlatformPresenter<ZelleClaimState>
{
    private readonly GetPlatformZelleClaimUseCase _get;

    private readonly ConfirmPlatformZelleClaimUseCase _confirm;

    private readonly RejectPlatformZelleClaimUseCase _reject;

    private Func<Task<bool>>? _waiting;

    public ZelleClaimPresenter(GetPlatformZelleClaimUseCase get, ConfirmPlatformZelleClaimUseCase confirm, RejectPlatformZelleClaimUseCase reject, PlatformUseCaseScope scope, ILogger<ZelleClaimPresenter> logger)
        : base(scope, ZelleClaimState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
    }

    public async Task LoadAsync(Guid claimId)
    {
        GetPlatformZelleClaimResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformZelleClaimRequest { ClaimId = claimId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Detail = response.Detail, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public Task<bool> ConfirmAsync(Guid claimId, decimal? amount, DateTime? receivedOn, string? note)
    {
        DateTimeOffset? received = receivedOn is { } day ? new DateTimeOffset(DateTime.SpecifyKind(day.Date, DateTimeKind.Utc)) : null;
        return RunAsync(claimId,
            () => RunUseCaseAsync(async (context, cancellationToken) => (await _confirm.ExecuteAsync(context,
                new ConfirmPlatformZelleClaimRequest { ClaimId = claimId, ReceivedAmount = amount, ReceivedUtc = received, Note = note }, cancellationToken)).Error),
            "ui.zelle.confirmed");
    }

    public Task<bool> RejectAsync(Guid claimId, string? reason, string? note)
    {
        return RunAsync(claimId,
            () => RunUseCaseAsync(async (context, cancellationToken) => (await _reject.ExecuteAsync(context,
                new RejectPlatformZelleClaimRequest { ClaimId = claimId, Reason = reason, Note = note }, cancellationToken)).Error),
            "ui.zelle.rejected");
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

    private async Task<bool> RunAsync(Guid claimId, Func<Task<Error?>> action, string doneKey)
    {
        Error? error = await action();

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            _waiting = () => RunAsync(claimId, action, doneKey);
            SetState(State with { StepUpPending = true, Error = null, DoneKey = null });
            return false;
        }

        await LoadAsync(claimId);
        SetState(State with { StepUpPending = false, Error = error, DoneKey = error is null ? doneKey : null });
        return error is null;
    }
}
