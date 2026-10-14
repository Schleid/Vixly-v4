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

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformPromotion;

// =========================================================
// FILE: UpdatePlatformPromotionUseCase.cs
//
// PURPOSE:
// Changes a draft or rejected promotion; it becomes a draft again.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return UpdatePlatformPromotionResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return UpdatePlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        if (request.Terms is null)
        {
            return UpdatePlatformPromotionResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        try
        {
            promotion.Edit(
                request.Terms.ToTerms(utcNow),
                request.ValidFromUtc,
                request.ValidToUtc,
                request.PlanIds ?? Array.Empty<Guid>(),
                request.Term,
                request.AtRegistration,
                request.ByStaff,
                request.RedemptionLimit,
                utcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformPromotionResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.promotions.updated", PlatformPermissions.PromotionsManage, nameof(PlatformPromotion), promotion.Id, after: new { code = promotion.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformPromotionResponse.Success()
            : UpdatePlatformPromotionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
