using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegal;
using Vixly.Platform.Application.UseCases.Legal.RecordPlatformTermsAcceptance;
using Vixly.Platform.Application.UseCases.Legal.SendPlatformTermsReminder;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: TenantLegalPresenter.cs
//
// PURPOSE:
// Behaviour of the Legal section of the tenant page (Slice 8).
//
// RESPONSIBILITIES:
// - Load the tenant's legal status, acceptances and reminders
// - Send a reminder; record an offline acceptance (step-up)
//
// IMPORTANT NOTES:
// - The waiting action runs again once step-up is done
// =========================================================
public sealed class TenantLegalPresenter : PlatformPresenter<TenantLegalState>
{
    private readonly GetPlatformTenantLegalUseCase _get;

    private readonly SendPlatformTermsReminderUseCase _remind;

    private readonly RecordPlatformTermsAcceptanceUseCase _record;

    private Func<Task<bool>>? _waiting;

    public TenantLegalPresenter(GetPlatformTenantLegalUseCase get, SendPlatformTermsReminderUseCase remind, RecordPlatformTermsAcceptanceUseCase record, PlatformUseCaseScope scope, ILogger<TenantLegalPresenter> logger)
        : base(scope, TenantLegalState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _remind = remind ?? throw new ArgumentNullException(nameof(remind));
        _record = record ?? throw new ArgumentNullException(nameof(record));
    }

    public async Task LoadAsync(Guid tenantId)
    {
        GetPlatformTenantLegalResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantLegalRequest { PlatformTenantId = tenantId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Detail = response.Detail, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public Task<bool> RemindAsync(Guid tenantId, string? reason)
    {
        return RunAsync(tenantId, () => RunUseCaseAsync(async (context, cancellationToken) =>
            (await _remind.ExecuteAsync(context, new SendPlatformTermsReminderRequest { PlatformTenantId = tenantId, Reason = reason }, cancellationToken)).Error), "ui.legal.reminder_sent");
    }

    public Task<bool> RecordAsync(Guid tenantId, string? language, string? ownerName, string? reason, byte[]? evidence, string? contentType)
    {
        return RunAsync(tenantId, () => RunUseCaseAsync(async (context, cancellationToken) => (await _record.ExecuteAsync(context, new RecordPlatformTermsAcceptanceRequest
        {
            PlatformTenantId = tenantId, Language = language, OwnerName = ownerName, Reason = reason, Evidence = evidence, EvidenceContentType = contentType
        }, cancellationToken)).Error), "ui.legal.acceptance_recorded");
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

    public void Fail(Error error)
    {
        SetState(State with { Error = error, DoneKey = null });
    }

    private async Task<bool> RunAsync(Guid tenantId, Func<Task<Error?>> action, string doneKey)
    {
        Error? error = await action();

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            _waiting = () => RunAsync(tenantId, action, doneKey);
            SetState(State with { StepUpPending = true, Error = null, DoneKey = null });
            return false;
        }

        await LoadAsync(tenantId);
        SetState(State with { StepUpPending = false, Error = error, DoneKey = error is null ? doneKey : null });
        return error is null;
    }
}
