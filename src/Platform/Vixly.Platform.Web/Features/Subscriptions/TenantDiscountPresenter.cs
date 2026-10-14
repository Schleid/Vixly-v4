using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Subscriptions.ApplyPlatformPromotion;
using Vixly.Platform.Application.UseCases.Subscriptions.ApprovePlatformDiscount;
using Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformDiscount;
using Vixly.Platform.Application.UseCases.Subscriptions.GrantPlatformDiscount;
using Vixly.Platform.Application.UseCases.Subscriptions.RejectPlatformDiscount;
using Vixly.Platform.Web.Features.Sales;

namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantDiscountPresenter.cs
//
// PURPOSE:
// The Discount panel of the tenant page (D-S18 to D-S24).
//
// RESPONSIBILITIES:
// - Grant, approve, reject and end a discount; apply a promotion code
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// - Whether a grant needs approval is decided by the Sales settings, never here
// =========================================================
public sealed class TenantDiscountPresenter : SalesActionPresenter
{
    private readonly GrantPlatformDiscountUseCase _grant;

    private readonly ApprovePlatformDiscountUseCase _approve;

    private readonly RejectPlatformDiscountUseCase _reject;

    private readonly EndPlatformDiscountUseCase _end;

    private readonly ApplyPlatformPromotionUseCase _apply;

    public TenantDiscountPresenter(
        GrantPlatformDiscountUseCase grant,
        ApprovePlatformDiscountUseCase approve,
        RejectPlatformDiscountUseCase reject,
        EndPlatformDiscountUseCase end,
        ApplyPlatformPromotionUseCase apply,
        PlatformUseCaseScope scope,
        ILogger<TenantDiscountPresenter> logger)
        : base(scope, logger)
    {
        _grant = grant ?? throw new ArgumentNullException(nameof(grant));
        _approve = approve ?? throw new ArgumentNullException(nameof(approve));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
        _end = end ?? throw new ArgumentNullException(nameof(end));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }

    public async Task<bool> GrantAsync(Guid platformTenantId, DiscountTermsModel terms, string? reason)
    {
        ArgumentNullException.ThrowIfNull(terms);

        bool waits = false;
        bool done = await ActAsync("ui.discounts.applied", async (context, cancellationToken) =>
        {
            GrantPlatformDiscountResponse response = await _grant.ExecuteAsync(
                context,
                new GrantPlatformDiscountRequest { PlatformTenantId = platformTenantId, Terms = terms.ToInput(), Reason = reason ?? string.Empty },
                cancellationToken);
            waits = response.NeedsApproval;
            return (response.Error, null);
        });

        if (done && waits)
        {
            SetState(State with { DoneKey = "ui.discounts.sent_for_approval" });
        }

        return done;
    }

    public Task<bool> ApproveAsync(Guid platformTenantId, Guid discountId, string? reason)
    {
        return ActAsync("ui.discounts.approved", async (context, cancellationToken) => ((await _approve.ExecuteAsync(
            context, new ApprovePlatformDiscountRequest { PlatformTenantId = platformTenantId, DiscountId = discountId, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> RejectAsync(Guid platformTenantId, Guid discountId, string? reason)
    {
        return ActAsync("ui.discounts.rejected", async (context, cancellationToken) => ((await _reject.ExecuteAsync(
            context, new RejectPlatformDiscountRequest { PlatformTenantId = platformTenantId, DiscountId = discountId, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> EndAsync(Guid platformTenantId, string? reason)
    {
        return ActAsync("ui.discounts.ended", async (context, cancellationToken) => ((await _end.ExecuteAsync(
            context, new EndPlatformDiscountRequest { PlatformTenantId = platformTenantId, Reason = reason ?? string.Empty }, cancellationToken)).Error, null));
    }

    public Task<bool> ApplyPromotionAsync(Guid platformTenantId, string? code)
    {
        return ActAsync("ui.discounts.promotion_applied", async (context, cancellationToken) => ((await _apply.ExecuteAsync(
            context, new ApplyPlatformPromotionRequest { PlatformTenantId = platformTenantId, Code = code ?? string.Empty }, cancellationToken)).Error, null));
    }
}
