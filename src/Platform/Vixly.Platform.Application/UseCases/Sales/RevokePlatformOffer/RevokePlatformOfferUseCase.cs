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

namespace Vixly.Platform.Application.UseCases.Sales.RevokePlatformOffer;

// =========================================================
// FILE: RevokePlatformOfferUseCase.cs
//
// PURPOSE:
// Revokes an offer so its link stops working (D-S21).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RevokePlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RevokePlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RevokePlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RevokePlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.OffersManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return RevokePlatformOfferResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return RevokePlatformOfferResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return RevokePlatformOfferResponse.Failure(PlatformSalesErrors.ReasonRequired);
        }

        try
        {
            offer.Revoke(context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return RevokePlatformOfferResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.offers.revoked", PlatformPermissions.OffersManage, nameof(PlatformOffer), offer.Id, after: new { status = offer.Status.ToString() }, reason: request.Reason.Trim());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RevokePlatformOfferResponse.Success()
            : RevokePlatformOfferResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
