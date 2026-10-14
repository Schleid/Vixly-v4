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

namespace Vixly.Platform.Application.UseCases.Sales.PreparePlatformOffer;

// =========================================================
// FILE: PreparePlatformOfferUseCase.cs
//
// PURPOSE:
// Prepares an offer link with the agreed terms, as a draft (D-S21).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Nothing is issued yet: Submit issues the link or asks the sales manager
// =========================================================
public sealed class PreparePlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PreparePlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformPlanRepository plans,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PreparePlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        PreparePlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.OffersManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return PreparePlatformOfferResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _sales.UtcNow;
        PlatformPlan? plan = await _plans.GetAsync(request.PlatformPlanId, cancellationToken);

        if (PlatformOfferPlanCheck.Refusal(plan, request.Term, request.Licences, request.StartTrial) is { } refusedPlan)
        {
            return PreparePlatformOfferResponse.Failure(refusedPlan);
        }

        PlatformOffer offer;

        try
        {
            offer = PlatformOffer.Prepare(
                request.CompanyName,
                request.BoundEmail,
                request.Reason,
                request.PlatformPlanId,
                request.Term,
                request.Licences,
                request.Discount?.ToTerms(utcNow),
                request.SetupFeeOverride,
                (request.Charges ?? Array.Empty<PlatformOfferChargeItem>()).Select(item => (item.Description, item.Amount)).ToArray(),
                request.StartTrial,
                request.ExpiresUtc,
                request.UsesAllowed,
                context.PlatformUserId,
                utcNow);
        }
        catch (DomainException exception)
        {
            return PreparePlatformOfferResponse.Failure(exception.Error);
        }

        _sales.Offers.Add(offer);
        _sales.Audit(context, "platform.offers.prepared", PlatformPermissions.OffersManage, nameof(PlatformOffer), offer.Id, after: new { company = offer.CompanyName, plan = plan!.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PreparePlatformOfferResponse.Success(offer.Id)
            : PreparePlatformOfferResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
