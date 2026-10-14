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

namespace Vixly.Platform.Application.UseCases.Sales.ApprovePlatformOffer;

// =========================================================
// FILE: ApprovePlatformOfferUseCase.cs
//
// PURPOSE:
// Approves an offer waiting for the sales manager and issues its link (D-S24).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up; nobody approves their own offer
// - The preparer can get a new link later; the old one then stops working
// =========================================================
public sealed class ApprovePlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ApprovePlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ApprovePlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ApprovePlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.DiscountsApprove, requireStepUp: true, cancellationToken);

        if (refused is not null)
        {
            return ApprovePlatformOfferResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return ApprovePlatformOfferResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        (string Link, string Hash) issued = _sales.NewOfferLink();

        try
        {
            offer.Approve(context.PlatformUserId, request.Reason, issued.Hash, utcNow);
        }
        catch (DomainException exception)
        {
            return ApprovePlatformOfferResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.offers.approved", PlatformPermissions.DiscountsApprove, nameof(PlatformOffer), offer.Id, after: new { status = offer.Status.ToString() }, reason: offer.DecisionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ApprovePlatformOfferResponse.Success(issued.Link)
            : ApprovePlatformOfferResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
