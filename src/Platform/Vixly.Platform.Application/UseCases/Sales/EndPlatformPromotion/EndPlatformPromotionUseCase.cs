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

namespace Vixly.Platform.Application.UseCases.Sales.EndPlatformPromotion;

// =========================================================
// FILE: EndPlatformPromotionUseCase.cs
//
// PURPOSE:
// Ends a promotion for good.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Existing discounts keep running to their own end
// =========================================================
public sealed class EndPlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public EndPlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EndPlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        EndPlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return EndPlatformPromotionResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return EndPlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        try
        {
            promotion.End(utcNow);
        }
        catch (DomainException exception)
        {
            return EndPlatformPromotionResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.promotions.ended", PlatformPermissions.PromotionsManage, nameof(PlatformPromotion), promotion.Id, after: new { status = promotion.Status.ToString() }, reason: promotion.DecisionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndPlatformPromotionResponse.Success()
            : EndPlatformPromotionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
