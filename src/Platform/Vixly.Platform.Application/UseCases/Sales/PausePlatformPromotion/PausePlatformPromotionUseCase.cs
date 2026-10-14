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

namespace Vixly.Platform.Application.UseCases.Sales.PausePlatformPromotion;

// =========================================================
// FILE: PausePlatformPromotionUseCase.cs
//
// PURPOSE:
// Pauses an active promotion.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs no approval; existing discounts are not touched
// =========================================================
public sealed class PausePlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PausePlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PausePlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        PausePlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return PausePlatformPromotionResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return PausePlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        try
        {
            promotion.Pause(utcNow);
        }
        catch (DomainException exception)
        {
            return PausePlatformPromotionResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.promotions.paused", PlatformPermissions.PromotionsManage, nameof(PlatformPromotion), promotion.Id, after: new { status = promotion.Status.ToString() }, reason: promotion.DecisionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PausePlatformPromotionResponse.Success()
            : PausePlatformPromotionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
