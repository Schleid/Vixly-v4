using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing.DecidePlatformDeviceRelease;
using Vixly.Platform.Application.UseCases.Licensing.GetPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.RevokePlatformDevice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: DevicePresenter.cs
//
// PURPOSE:
// Behaviour of the device page (Slice 7).
//
// RESPONSIBILITIES:
// - Load the device and its history
// - Approve or refuse a release over the allowance; revoke a device
//
// IMPORTANT NOTES:
// - Every action asks for step-up and needs a note
// =========================================================
public sealed class DevicePresenter : PlatformPresenter<DeviceState>
{
    public const string ApproveAction = "approve";

    public const string RefuseAction = "refuse";

    public const string RevokeAction = "revoke";

    private readonly GetPlatformDeviceUseCase _get;

    private readonly DecidePlatformDeviceReleaseUseCase _decide;

    private readonly RevokePlatformDeviceUseCase _revoke;

    public DevicePresenter(GetPlatformDeviceUseCase get, DecidePlatformDeviceReleaseUseCase decide, RevokePlatformDeviceUseCase revoke, PlatformUseCaseScope scope, ILogger<DevicePresenter> logger)
        : base(scope, DeviceState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _decide = decide ?? throw new ArgumentNullException(nameof(decide));
        _revoke = revoke ?? throw new ArgumentNullException(nameof(revoke));
    }

    public async Task LoadAsync(Guid deviceId)
    {
        GetPlatformDeviceResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformDeviceRequest { DeviceId = deviceId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Detail = response.Detail, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpAction = null });
    }

    public async Task<bool> RunAsync(Guid deviceId, string action, string? note)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => action == RevokeAction
            ? (await _revoke.ExecuteAsync(context, new RevokePlatformDeviceRequest { DeviceId = deviceId, Note = note }, cancellationToken)).Error
            : (await _decide.ExecuteAsync(context, new DecidePlatformDeviceReleaseRequest { DeviceId = deviceId, Approve = action == ApproveAction, Note = note }, cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpAction = action, Error = null, DoneKey = null });
            return false;
        }

        await LoadAsync(deviceId);
        SetState(State with
        {
            StepUpAction = null,
            Error = error,
            DoneKey = error is null ? "ui.device.done." + action : null
        });
        return error is null;
    }
}
