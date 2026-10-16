using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing.GetPlatformTenantLicensing;
using Vixly.Platform.Application.UseCases.Licensing.SendPlatformFirstDeviceLink;
using Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecovery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: TenantLicensingPresenter.cs
//
// PURPOSE:
// Behaviour of the Licensing section of the tenant page (Slice 7).
//
// RESPONSIBILITIES:
// - Load seats bought, in use, waiting and under review
// - Send the first-device link again; start owner recovery
//
// IMPORTANT NOTES:
// - Both actions ask for step-up; the link always goes to the owner's email, never to the screen
// =========================================================
public sealed class TenantLicensingPresenter : PlatformPresenter<TenantLicensingState>
{
    public const string FirstDeviceAction = "first";

    public const string RecoveryAction = "recovery";

    private readonly GetPlatformTenantLicensingUseCase _get;

    private readonly SendPlatformFirstDeviceLinkUseCase _first;

    private readonly StartPlatformOwnerRecoveryUseCase _recovery;

    public TenantLicensingPresenter(
        GetPlatformTenantLicensingUseCase get,
        SendPlatformFirstDeviceLinkUseCase first,
        StartPlatformOwnerRecoveryUseCase recovery,
        PlatformUseCaseScope scope,
        ILogger<TenantLicensingPresenter> logger)
        : base(scope, TenantLicensingState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
    }

    public async Task LoadAsync(Guid tenantId)
    {
        GetPlatformTenantLicensingResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantLicensingRequest { PlatformTenantId = tenantId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Summary = response.Summary, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpAction = null });
    }

    public async Task RunAsync(Guid tenantId, string action)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => action == RecoveryAction
            ? (await _recovery.ExecuteAsync(context, new StartPlatformOwnerRecoveryRequest { PlatformTenantId = tenantId }, cancellationToken)).Error
            : (await _first.ExecuteAsync(context, new SendPlatformFirstDeviceLinkRequest { PlatformTenantId = tenantId }, cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpAction = action, Error = null, DoneKey = null });
            return;
        }

        await LoadAsync(tenantId);
        SetState(State with
        {
            StepUpAction = null,
            Error = error,
            DoneKey = error is null ? (action == RecoveryAction ? "ui.licensing.recovery_sent" : "ui.licensing.first_link_sent") : null
        });
    }
}
