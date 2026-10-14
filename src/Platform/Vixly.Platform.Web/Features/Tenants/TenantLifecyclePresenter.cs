using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants.ActivatePlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.BanPlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.DeactivatePlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.ReactivatePlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.SuspendPlatformTenant;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantLifecyclePresenter.cs
//
// PURPOSE:
// Behaviour of the lifecycle panel (D-T1, D-T2).
//
// RESPONSIBILITIES:
// - Confirm a move, ask for step-up when needed, apply it
// - Report success so the page reloads
//
// IMPORTANT NOTES:
// - Suspend, reactivate, deactivate and ban need step-up; the
//   same move is retried after the user confirms
// =========================================================
public sealed class TenantLifecyclePresenter : PlatformPresenter<TenantLifecycleState>
{
    private readonly ActivatePlatformTenantUseCase _activate;

    private readonly SuspendPlatformTenantUseCase _suspend;

    private readonly ReactivatePlatformTenantUseCase _reactivate;

    private readonly DeactivatePlatformTenantUseCase _deactivate;

    private readonly BanPlatformTenantUseCase _ban;

    public TenantLifecyclePresenter(
        ActivatePlatformTenantUseCase activate,
        SuspendPlatformTenantUseCase suspend,
        ReactivatePlatformTenantUseCase reactivate,
        DeactivatePlatformTenantUseCase deactivate,
        BanPlatformTenantUseCase ban,
        PlatformUseCaseScope scope,
        ILogger<TenantLifecyclePresenter> logger)
        : base(scope, TenantLifecycleState.Initial, logger)
    {
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _suspend = suspend ?? throw new ArgumentNullException(nameof(suspend));
        _reactivate = reactivate ?? throw new ArgumentNullException(nameof(reactivate));
        _deactivate = deactivate ?? throw new ArgumentNullException(nameof(deactivate));
        _ban = ban ?? throw new ArgumentNullException(nameof(ban));
    }

    public void Start(TenantMove move)
    {
        SetState(new TenantLifecycleState(move, false, null, null));
    }

    public void Cancel()
    {
        SetState(TenantLifecycleState.Initial);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> ApplyAsync(Guid platformTenantId, PlatformSuspensionCategory category, string? reason)
    {
        if (State.Confirming is not TenantMove move)
        {
            return false;
        }

        Error? error = await RunUseCaseAsync((context, cancellationToken) => RunAsync(move, context, platformTenantId, category, reason ?? string.Empty, cancellationToken));

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(error is null
            ? new TenantLifecycleState(null, false, "ui.tenants.done." + move.ToString().ToLowerInvariant(), null)
            : State with { StepUpPending = false, Error = error });

        return error is null;
    }

    private async Task<Error?> RunAsync(
        TenantMove move,
        PlatformAuthorizationContext context,
        Guid id,
        PlatformSuspensionCategory category,
        string reason,
        CancellationToken cancellationToken)
    {
        return move switch
        {
            TenantMove.Activate => (await _activate.ExecuteAsync(context, new ActivatePlatformTenantRequest { PlatformTenantId = id }, cancellationToken)).Error,
            TenantMove.Suspend => (await _suspend.ExecuteAsync(context, new SuspendPlatformTenantRequest { PlatformTenantId = id, Category = category, Reason = reason }, cancellationToken)).Error,
            TenantMove.Reactivate => (await _reactivate.ExecuteAsync(context, new ReactivatePlatformTenantRequest { PlatformTenantId = id, Reason = reason }, cancellationToken)).Error,
            TenantMove.Deactivate => (await _deactivate.ExecuteAsync(context, new DeactivatePlatformTenantRequest { PlatformTenantId = id, Reason = reason }, cancellationToken)).Error,
            _ => (await _ban.ExecuteAsync(context, new BanPlatformTenantRequest { PlatformTenantId = id, Reason = reason }, cancellationToken)).Error
        };
    }
}
