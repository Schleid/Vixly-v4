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

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformOffer;

// =========================================================
// FILE: UpdatePlatformOfferUseCase.cs
//
// PURPOSE:
// Changes a draft or rejected offer; it becomes a draft again.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformPlanRepository plans,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.OffersManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return UpdatePlatformOfferResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return UpdatePlatformOfferResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        PlatformPlan? plan = await _plans.GetAsync(request.PlatformPlanId, cancellationToken);

        if (PlatformOfferPlanCheck.Refusal(plan, request.Term, request.Licences, request.StartTrial) is { } refusedPlan)
        {
            return UpdatePlatformOfferResponse.Failure(refusedPlan);
        }

        try
        {
            offer.Edit(
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
                utcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformOfferResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.offers.updated", PlatformPermissions.OffersManage, nameof(PlatformOffer), offer.Id, after: new { company = offer.CompanyName });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformOfferResponse.Success()
            : UpdatePlatformOfferResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
