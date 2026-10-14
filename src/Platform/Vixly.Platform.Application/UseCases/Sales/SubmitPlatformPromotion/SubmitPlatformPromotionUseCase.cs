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

namespace Vixly.Platform.Application.UseCases.Sales.SubmitPlatformPromotion;

// =========================================================
// FILE: SubmitPlatformPromotionUseCase.cs
//
// PURPOSE:
// Submits a promotion for CFO approval (D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The CFO is asked by email
// =========================================================
public sealed class SubmitPlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SubmitPlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SubmitPlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SubmitPlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return SubmitPlatformPromotionResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return SubmitPlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        try
        {
            promotion.Submit(context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return SubmitPlatformPromotionResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.promotions.submitted", PlatformPermissions.PromotionsManage, nameof(PlatformPromotion), promotion.Id, after: new { status = promotion.Status.ToString() }, reason: promotion.DecisionReason);
        _sales.RequestApproval(PlatformApprovalKind.Promotion, promotion.Id, PlatformPermissions.PromotionsApprove, promotion.Code, context.PlatformUserId);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SubmitPlatformPromotionResponse.Success()
            : SubmitPlatformPromotionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
