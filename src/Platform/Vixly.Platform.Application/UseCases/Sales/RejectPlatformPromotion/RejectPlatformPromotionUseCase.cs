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

namespace Vixly.Platform.Application.UseCases.Sales.RejectPlatformPromotion;

// =========================================================
// FILE: RejectPlatformPromotionUseCase.cs
//
// PURPOSE:
// Rejects a submitted promotion with a reason (D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up
// - Nobody rejects their own submission
// =========================================================
public sealed class RejectPlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RejectPlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RejectPlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RejectPlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsApprove, requireStepUp: true, cancellationToken);

        if (refused is not null)
        {
            return RejectPlatformPromotionResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return RejectPlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        try
        {
            promotion.Reject(context.PlatformUserId, request.Reason, utcNow);
        }
        catch (DomainException exception)
        {
            return RejectPlatformPromotionResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.promotions.rejected", PlatformPermissions.PromotionsApprove, nameof(PlatformPromotion), promotion.Id, after: new { status = promotion.Status.ToString() }, reason: promotion.DecisionReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RejectPlatformPromotionResponse.Success()
            : RejectPlatformPromotionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
