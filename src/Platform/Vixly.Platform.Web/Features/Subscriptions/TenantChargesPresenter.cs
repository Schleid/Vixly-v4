using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Subscriptions.AddPlatformOneTimeCharge;
using Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformOneTimeCharge;
using Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSetupFee;
using Vixly.Platform.Web.Features.Sales;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantChargesPresenter.cs
//
// PURPOSE:
// The One-time charges and Setup fee panel of the tenant page (D-S23).
//
// RESPONSIBILITIES:
// - Add and cancel one-time charges; change, waive or reset the setup fee
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class TenantChargesPresenter : SalesActionPresenter
{
    private readonly AddPlatformOneTimeChargeUseCase _add;

    private readonly CancelPlatformOneTimeChargeUseCase _cancel;

    private readonly ChangePlatformSetupFeeUseCase _setupFee;

    public TenantChargesPresenter(
        AddPlatformOneTimeChargeUseCase add,
        CancelPlatformOneTimeChargeUseCase cancel,
        ChangePlatformSetupFeeUseCase setupFee,
        PlatformUseCaseScope scope,
        ILogger<TenantChargesPresenter> logger)
        : base(scope, logger)
    {
        _add = add ?? throw new ArgumentNullException(nameof(add));
        _cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        _setupFee = setupFee ?? throw new ArgumentNullException(nameof(setupFee));
    }

    public Task<bool> AddAsync(Guid platformTenantId, string? description, decimal amount, string? reason)
    {
        return ActAsync("ui.charges.added", async (context, cancellationToken) => ((await _add.ExecuteAsync(
            context,
            new AddPlatformOneTimeChargeRequest { PlatformTenantId = platformTenantId, Description = description ?? string.Empty, Amount = amount, Reason = reason ?? string.Empty },
            cancellationToken)).Error, null));
    }

    public Task<bool> CancelAsync(Guid platformTenantId, Guid chargeId, string? reason)
    {
        return ActAsync("ui.charges.cancelled", async (context, cancellationToken) => ((await _cancel.ExecuteAsync(
            context, new CancelPlatformOneTimeChargeRequest { PlatformTenantId = platformTenantId, ChargeId = chargeId, Reason = reason ?? string.Empty }, cancellationToken)).Error, null));
    }

    public Task<bool> ChangeSetupFeeAsync(Guid platformTenantId, decimal? amount, string? reason)
    {
        return ActAsync("ui.charges.setup_fee_changed", async (context, cancellationToken) => ((await _setupFee.ExecuteAsync(
            context, new ChangePlatformSetupFeeRequest { PlatformTenantId = platformTenantId, Amount = amount, Reason = reason ?? string.Empty }, cancellationToken)).Error, null));
    }
}
