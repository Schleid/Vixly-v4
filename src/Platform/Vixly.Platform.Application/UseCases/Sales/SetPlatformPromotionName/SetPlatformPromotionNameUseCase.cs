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

namespace Vixly.Platform.Application.UseCases.Sales.SetPlatformPromotionName;

// =========================================================
// FILE: SetPlatformPromotionNameUseCase.cs
//
// PURPOSE:
// Sets a promotion's name in one language (ADR-0005).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Allowed in any status: a name is not a commercial term
// =========================================================
public sealed class SetPlatformPromotionNameUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformPromotionNameUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformPromotionNameResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformPromotionNameRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PromotionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return SetPlatformPromotionNameResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return SetPlatformPromotionNameResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        LanguageCode language;

        try
        {
            language = LanguageCode.Create(request.Language);
            promotion.SetName(language, request.Name, utcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformPromotionNameResponse.Failure(exception.Error);
        }

        _sales.Audit(context, "platform.promotions.name_set", PlatformPermissions.PromotionsManage, nameof(PlatformPromotion), promotion.Id, after: new { language = language.Value });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetPlatformPromotionNameResponse.Success()
            : SetPlatformPromotionNameResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
