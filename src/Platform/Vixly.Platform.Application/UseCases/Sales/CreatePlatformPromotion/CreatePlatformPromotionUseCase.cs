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

namespace Vixly.Platform.Application.UseCases.Sales.CreatePlatformPromotion;

// =========================================================
// FILE: CreatePlatformPromotionUseCase.cs
//
// PURPOSE:
// Designs a new promotion as a draft (D-S20, D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A promotion goes live only after CFO approval (D-S26)
// - The unique index on the code decides a race
// =========================================================
public sealed class CreatePlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CreatePlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreatePlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CreatePlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return CreatePlatformPromotionResponse.Failure(refused);
        }

        if (request.Terms is null)
        {
            return CreatePlatformPromotionResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        PlatformPromotion promotion;

        try
        {
            promotion = PlatformPromotion.Create(
                request.Code,
                request.Name,
                request.Terms.ToTerms(_sales.UtcNow),
                request.ValidFromUtc,
                request.ValidToUtc,
                request.PlanIds ?? Array.Empty<Guid>(),
                request.Term,
                request.AtRegistration,
                request.ByStaff,
                request.RedemptionLimit,
                context.PlatformUserId,
                _sales.UtcNow);
        }
        catch (DomainException exception)
        {
            return CreatePlatformPromotionResponse.Failure(exception.Error);
        }

        if (await _sales.Promotions.CodeExistsAsync(promotion.Code, cancellationToken))
        {
            return CreatePlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionCodeTaken);
        }

        _sales.Promotions.Add(promotion);
        _sales.Audit(context, "platform.promotions.created", PlatformPermissions.PromotionsManage, nameof(PlatformPromotion), promotion.Id, after: new { code = promotion.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CreatePlatformPromotionResponse.Success(promotion.Id)
            : CreatePlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionCodeTaken);
    }
}
