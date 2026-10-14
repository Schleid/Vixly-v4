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

namespace Vixly.Platform.Application.UseCases.Sales.ReplacePlatformOfferLink;

// =========================================================
// FILE: ReplacePlatformOfferLinkUseCase.cs
//
// PURPOSE:
// Issues a new link for an issued offer; the old link stops working.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - How staff get the link again: it is never stored, so it is replaced
// =========================================================
public sealed class ReplacePlatformOfferLinkUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ReplacePlatformOfferLinkUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReplacePlatformOfferLinkResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReplacePlatformOfferLinkRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.OffersManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return ReplacePlatformOfferLinkResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return ReplacePlatformOfferLinkResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        (string Link, string Hash) issued = _sales.NewOfferLink();

        try
        {
            offer.ReplaceLink(issued.Hash, utcNow);
        }
        catch (DomainException exception)
        {
            return ReplacePlatformOfferLinkResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.offers.link_replaced", PlatformPermissions.OffersManage, nameof(PlatformOffer), offer.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ReplacePlatformOfferLinkResponse.Success(issued.Link)
            : ReplacePlatformOfferLinkResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
