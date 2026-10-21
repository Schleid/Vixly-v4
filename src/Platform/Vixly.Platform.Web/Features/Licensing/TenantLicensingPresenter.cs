using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing.DecidePlatformSecondFactorReset;
using Vixly.Platform.Application.UseCases.Licensing.GetPlatformTenantLicensing;
using Vixly.Platform.Application.UseCases.Licensing.RequestPlatformSecondFactorReset;
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
// - Send the first-device link again; start owner recovery with the identity check (D-O13)
// - Ask for, approve or refuse a reset of the owner's second factor (second staff member)
//
// IMPORTANT NOTES:
// - Both actions ask for step-up; the link always goes to the owner's email, never to the screen
// =========================================================
public sealed class TenantLicensingPresenter : PlatformPresenter<TenantLicensingState>
{
    public const string FirstDeviceAction = "first";

    public const string RecoveryAction = "recovery";

    public const string ResetAction = "reset";

    public const string ApproveAction = "approve:";

    public const string RefuseAction = "refuse:";

    private readonly GetPlatformTenantLicensingUseCase _get;

    private readonly SendPlatformFirstDeviceLinkUseCase _first;

    private readonly StartPlatformOwnerRecoveryUseCase _recovery;

    private readonly RequestPlatformSecondFactorResetUseCase _reset;

    private readonly DecidePlatformSecondFactorResetUseCase _decide;

    // What staff typed, kept while a step-up is asked, then sent once.
    private OwnerSupportInput? _pending;

    public TenantLicensingPresenter(
        GetPlatformTenantLicensingUseCase get,
        SendPlatformFirstDeviceLinkUseCase first,
        StartPlatformOwnerRecoveryUseCase recovery,
        RequestPlatformSecondFactorResetUseCase reset,
        DecidePlatformSecondFactorResetUseCase decide,
        PlatformUseCaseScope scope,
        ILogger<TenantLicensingPresenter> logger)
        : base(scope, TenantLicensingState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        _reset = reset ?? throw new ArgumentNullException(nameof(reset));
        _decide = decide ?? throw new ArgumentNullException(nameof(decide));
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
        _pending = null;
        SetState(State with { StepUpAction = null });
    }

    public async Task RunAsync(Guid tenantId, string action, OwnerSupportInput? input = null)
    {
        OwnerSupportInput typed = input ?? _pending ?? new OwnerSupportInput(null, null, null, null);
        _pending = typed;

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            if (action == RecoveryAction)
            {
                return (await _recovery.ExecuteAsync(context, new StartPlatformOwnerRecoveryRequest
                {
                    PlatformTenantId = tenantId,
                    Reason = typed.Reason ?? string.Empty,
                    IdentityCheck = typed.IdentityCheck ?? string.Empty,
                    ReplaceDeviceId = typed.ReplaceDeviceId
                }, cancellationToken)).Error;
            }

            if (action == ResetAction)
            {
                return (await _reset.ExecuteAsync(context, new RequestPlatformSecondFactorResetRequest
                {
                    PlatformTenantId = tenantId,
                    Reason = typed.Reason ?? string.Empty,
                    IdentityCheck = typed.IdentityCheck ?? string.Empty
                }, cancellationToken)).Error;
            }

            if (action.StartsWith(ApproveAction, StringComparison.Ordinal) || action.StartsWith(RefuseAction, StringComparison.Ordinal))
            {
                bool approve = action.StartsWith(ApproveAction, StringComparison.Ordinal);
                Guid requestId = Guid.Parse(action[(approve ? ApproveAction.Length : RefuseAction.Length)..]);
                return (await _decide.ExecuteAsync(context, new DecidePlatformSecondFactorResetRequest { RequestId = requestId, Approve = approve, Note = typed.Note }, cancellationToken)).Error;
            }

            return (await _first.ExecuteAsync(context, new SendPlatformFirstDeviceLinkRequest { PlatformTenantId = tenantId }, cancellationToken)).Error;
        });

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpAction = action, Error = null, DoneKey = null });
            return;
        }

        _pending = null;
        await LoadAsync(tenantId);
        SetState(State with
        {
            StepUpAction = null,
            Error = error,
            DoneKey = error is null ? DoneKeyFor(action) : null
        });
    }

    private static string DoneKeyFor(string action)
    {
        if (action.StartsWith(ApproveAction, StringComparison.Ordinal))
        {
            return "ui.licensing.reset_approved";
        }

        if (action.StartsWith(RefuseAction, StringComparison.Ordinal))
        {
            return "ui.licensing.reset_refused";
        }

        return action switch
        {
            RecoveryAction => "ui.licensing.recovery_sent",
            ResetAction => "ui.licensing.reset_requested",
            _ => "ui.licensing.first_link_sent"
        };
    }
}
