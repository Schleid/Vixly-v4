using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleAccount;
using Vixly.Platform.Application.UseCases.Payments.SetPlatformZelleQrCode;
using Vixly.Platform.Application.UseCases.Payments.UpdatePlatformZelleAccount;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleSettingsPresenter.cs
//
// PURPOSE:
// Behaviour of the Zelle settings (Slice 5 update).
//
// RESPONSIBILITIES:
// - Load the Zelle details and QR image
// - Save the details; replace or remove the QR image
//
// IMPORTANT NOTES:
// - Saving asks for step-up; the waiting action runs again once step-up is done
// =========================================================
public sealed class ZelleSettingsPresenter : PlatformPresenter<ZelleSettingsState>
{
    private readonly GetPlatformZelleAccountUseCase _get;

    private readonly UpdatePlatformZelleAccountUseCase _update;

    private readonly SetPlatformZelleQrCodeUseCase _qr;

    private Func<Task>? _waiting;

    public ZelleSettingsPresenter(GetPlatformZelleAccountUseCase get, UpdatePlatformZelleAccountUseCase update, SetPlatformZelleQrCodeUseCase qr, PlatformUseCaseScope scope, ILogger<ZelleSettingsPresenter> logger)
        : base(scope, ZelleSettingsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _qr = qr ?? throw new ArgumentNullException(nameof(qr));
    }

    public async Task LoadAsync()
    {
        GetPlatformZelleAccountResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformZelleAccountRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Account = response.Account, QrCode = response.QrCode, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public Task SaveAsync(bool enabled, string? recipient, string? email, string? tag, string? phone)
    {
        return RunAsync(
            () => RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(context,
                new UpdatePlatformZelleAccountRequest { Enabled = enabled, RecipientName = recipient, Email = email, Tag = tag, Phone = phone }, cancellationToken)).Error),
            "ui.zelle_settings.saved");
    }

    public Task SetQrCodeAsync(byte[]? content, string? contentType)
    {
        return RunAsync(
            () => RunUseCaseAsync(async (context, cancellationToken) => (await _qr.ExecuteAsync(context,
                new SetPlatformZelleQrCodeRequest { Content = content, ContentType = contentType }, cancellationToken)).Error),
            content is null ? "ui.zelle_settings.qr_removed" : "ui.zelle_settings.qr_saved");
    }

    public void Fail(Error error)
    {
        SetState(State with { Error = error, DoneKey = null });
    }

    public Task ConfirmStepUpAsync()
    {
        Func<Task>? waiting = _waiting;
        _waiting = null;
        return waiting?.Invoke() ?? Task.CompletedTask;
    }

    public void CancelStepUp()
    {
        _waiting = null;
        SetState(State with { StepUpPending = false });
    }

    private async Task RunAsync(Func<Task<Error?>> action, string doneKey)
    {
        Error? error = await action();

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            _waiting = () => RunAsync(action, doneKey);
            SetState(State with { StepUpPending = true, Error = null, DoneKey = null });
            return;
        }

        if (error is null)
        {
            await LoadAsync();
        }

        SetState(State with { StepUpPending = false, Error = error, DoneKey = error is null ? doneKey : null });
    }
}
