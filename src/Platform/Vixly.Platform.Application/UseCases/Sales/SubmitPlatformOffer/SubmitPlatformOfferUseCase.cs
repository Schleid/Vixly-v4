using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Sales.SubmitPlatformOffer;

// =========================================================
// FILE: SubmitPlatformOfferUseCase.cs
//
// PURPOSE:
// Issues the offer link at once when within the limits, otherwise asks the sales manager (D-S22, D-S24).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The link is returned once and never stored; only its hash is
// - Empty Sales settings send every offer to approval (D-S22)
// =========================================================
public sealed class SubmitPlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SubmitPlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformPlanRepository plans,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SubmitPlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SubmitPlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.OffersManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return SubmitPlatformOfferResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return SubmitPlatformOfferResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        PlatformPlan? plan = await _plans.GetAsync(offer.PlatformPlanId, cancellationToken);

        if (PlatformOfferPlanCheck.Refusal(plan, offer.Term, offer.Licences, offer.StartTrial) is { } refusedPlan)
        {
            return SubmitPlatformOfferResponse.Failure(refusedPlan);
        }

        PlatformSalesSettings settings = await _sales.SettingsAsync(cancellationToken);
        bool needsApproval = settings.OfferNeedsApproval(offer.Discount, plan!.CurrencyCode, offer.ExpiresUtc, utcNow);
        string? link = null;

        try
        {
            if (needsApproval)
            {
                offer.Submit(true, null, utcNow);
            }
            else
            {
                (string Link, string Hash) issued = _sales.NewOfferLink();
                offer.Submit(false, issued.Hash, utcNow);
                link = issued.Link;
            }
        }
        catch (DomainException exception)
        {
            return SubmitPlatformOfferResponse.Failure(exception.Error);
        }

        _sales.Audit(context, needsApproval ? "platform.offers.submitted" : "platform.offers.issued", PlatformPermissions.OffersManage, nameof(PlatformOffer), offer.Id, after: new { status = offer.Status.ToString() });

        if (needsApproval)
        {
            _sales.RequestApproval(PlatformApprovalKind.Offer, offer.Id, PlatformPermissions.DiscountsApprove, offer.CompanyName, context.PlatformUserId);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SubmitPlatformOfferResponse.Success(needsApproval, link)
            : SubmitPlatformOfferResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
