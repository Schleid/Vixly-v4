using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionsSettings;
using Vixly.Platform.Application.UseCases.Collections.UpdatePlatformCollectionsSettings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionsSettingsPresenter.cs
//
// PURPOSE:
// Behaviour of the Collections settings page (D-C12).
//
// RESPONSIBILITIES:
// - Load the settings; save them with step-up
//
// IMPORTANT NOTES:
// - "Stay read-only" saves the read-only days as empty: read-only until paid
// =========================================================
public sealed class CollectionsSettingsPresenter : PlatformPresenter<CollectionsSettingsState>
{
    private readonly GetPlatformCollectionsSettingsUseCase _get;

    private readonly UpdatePlatformCollectionsSettingsUseCase _update;

    public CollectionsSettingsPresenter(GetPlatformCollectionsSettingsUseCase get, UpdatePlatformCollectionsSettingsUseCase update, PlatformUseCaseScope scope, ILogger<CollectionsSettingsPresenter> logger)
        : base(scope, CollectionsSettingsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public async Task LoadAsync()
    {
        GetPlatformCollectionsSettingsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformCollectionsSettingsRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Settings = response.Settings, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task SaveAsync(CollectionsSettingsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        int[] retries = (input.RetryDays ?? string.Empty)
            .Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out int day) ? day : -1)
            .ToArray();
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(context, new UpdatePlatformCollectionsSettingsRequest
        {
            Enabled = input.Enabled,
            Reminder1Day = input.Reminder1Day,
            Reminder2Day = input.Reminder2Day,
            FinalWarningDay = input.FinalWarningDay,
            BlockDay = input.BlockDay,
            RetryDays = retries,
            MinimumNoticeDays = input.MinimumNoticeDays,
            BlockedReadOnlyDays = input.StayReadOnly ? null : input.BlockedReadOnlyDays,
            ExtensionMaxDays = input.ExtensionMaxDays,
            ExtensionsPerCase = input.ExtensionsPerCase,
            PendingPaymentReadOnlyDays = input.PendingPaymentReadOnlyDays,
            LongBlockDays = input.LongBlockDays
        }, cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return;
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? "ui.collections_settings.saved" : null, Error = error });

        if (error is null)
        {
            await LoadAsync();
        }
    }
}
