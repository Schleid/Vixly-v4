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

namespace Vixly.Platform.Application.UseCases.Sales.RejectPlatformOffer;

// =========================================================
// FILE: RejectPlatformOfferUseCase.cs
//
// PURPOSE:
// Rejects an offer waiting for the sales manager, with a reason (D-S24).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up; nobody rejects their own offer
// =========================================================
public sealed class RejectPlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RejectPlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RejectPlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RejectPlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.DiscountsApprove, requireStepUp: true, cancellationToken);

        if (refused is not null)
        {
            return RejectPlatformOfferResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return RejectPlatformOfferResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        try
        {
            offer.Reject(context.PlatformUserId, request.Reason, utcNow);
        }
        catch (DomainException exception)
        {
            return RejectPlatformOfferResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.offers.rejected", PlatformPermissions.DiscountsApprove, nameof(PlatformOffer), offer.Id, after: new { status = offer.Status.ToString() }, reason: offer.DecisionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RejectPlatformOfferResponse.Success()
            : RejectPlatformOfferResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
