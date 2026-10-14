using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Sales.ApprovePlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.RejectPlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.ReplacePlatformOfferLink;
using Vixly.Platform.Application.UseCases.Sales.RevokePlatformOffer;
using Vixly.Platform.Application.UseCases.Sales.SubmitPlatformOffer;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: OfferActionsPresenter.cs
//
// PURPOSE:
// Issues, approves, rejects, replaces and revokes an offer link (D-S21, D-S24).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class OfferActionsPresenter : SalesActionPresenter
{
    private readonly SubmitPlatformOfferUseCase _submit;

    private readonly ApprovePlatformOfferUseCase _approve;

    private readonly RejectPlatformOfferUseCase _reject;

    private readonly ReplacePlatformOfferLinkUseCase _replace;

    private readonly RevokePlatformOfferUseCase _revoke;

    public OfferActionsPresenter(
        SubmitPlatformOfferUseCase submit,
        ApprovePlatformOfferUseCase approve,
        RejectPlatformOfferUseCase reject,
        ReplacePlatformOfferLinkUseCase replace,
        RevokePlatformOfferUseCase revoke,
        PlatformUseCaseScope scope,
        ILogger<OfferActionsPresenter> logger)
        : base(scope, logger)
    {
        _submit = submit ?? throw new ArgumentNullException(nameof(submit));
        _approve = approve ?? throw new ArgumentNullException(nameof(approve));
        _reject = reject ?? throw new ArgumentNullException(nameof(reject));
        _replace = replace ?? throw new ArgumentNullException(nameof(replace));
        _revoke = revoke ?? throw new ArgumentNullException(nameof(revoke));
    }

    public Task<bool> SubmitAsync(Guid offerId)
    {
        return ActAsync("ui.offers.submitted", async (context, cancellationToken) =>
        {
            SubmitPlatformOfferResponse response = await _submit.ExecuteAsync(context, new SubmitPlatformOfferRequest { OfferId = offerId }, cancellationToken);
            return (response.Error, response.Link);
        });
    }

    public Task<bool> ApproveAsync(Guid offerId, string? reason)
    {
        return ActAsync("ui.offers.approved", async (context, cancellationToken) =>
        {
            ApprovePlatformOfferResponse response = await _approve.ExecuteAsync(context, new ApprovePlatformOfferRequest { OfferId = offerId, Reason = reason }, cancellationToken);
            return (response.Error, response.Link);
        });
    }

    public Task<bool> RejectAsync(Guid offerId, string? reason)
    {
        return ActAsync("ui.offers.rejected", async (context, cancellationToken) => ((await _reject.ExecuteAsync(
            context, new RejectPlatformOfferRequest { OfferId = offerId, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> ReplaceLinkAsync(Guid offerId)
    {
        return ActAsync("ui.offers.link_replaced", async (context, cancellationToken) =>
        {
            ReplacePlatformOfferLinkResponse response = await _replace.ExecuteAsync(context, new ReplacePlatformOfferLinkRequest { OfferId = offerId }, cancellationToken);
            return (response.Error, response.Link);
        });
    }

    public Task<bool> RevokeAsync(Guid offerId, string? reason)
    {
        return ActAsync("ui.offers.revoked", async (context, cancellationToken) => ((await _revoke.ExecuteAsync(
            context, new RevokePlatformOfferRequest { OfferId = offerId, Reason = reason ?? string.Empty }, cancellationToken)).Error, null));
    }
}
