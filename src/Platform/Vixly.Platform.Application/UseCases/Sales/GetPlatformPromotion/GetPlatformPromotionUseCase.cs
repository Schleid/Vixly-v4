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

namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformPromotion;

// =========================================================
// FILE: GetPlatformPromotionUseCase.cs
//
// PURPOSE:
// Reads one promotion for its console page.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - CanDecide is false for the person who submitted it
// =========================================================
public sealed class GetPlatformPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    private readonly IPlatformPlanRepository _plans;

    public GetPlatformPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries,
        IPlatformPlanRepository plans)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task<GetPlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.PlansView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return GetPlatformPromotionResponse.Failure(refused);
        }

        PlatformPromotion? promotion = await _sales.Promotions.GetAsync(request.PromotionId, cancellationToken);

        if (promotion is null)
        {
            return GetPlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        DateTimeOffset utcNow = _sales.UtcNow;

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        IReadOnlyDictionary<Guid, string> names = await _queries.StaffNamesAsync(
            new[] { promotion.DesignedByUserId, promotion.DecidedByUserId ?? Guid.Empty }.Where(id => id != Guid.Empty).Distinct(),
            cancellationToken);
        List<string> planNames = new();

        foreach (PlatformPromotionPlan item in promotion.Plans)
        {
            if (await _plans.GetAsync(item.PlatformPlanId, cancellationToken) is { } plan)
            {
                planNames.Add(plan.NameIn(language));
            }
        }

        PlatformPromotionDetail detail = new(
            promotion.Id,
            promotion.Code,
            promotion.NameIn(language),
            promotion.Names.ToDictionary(item => item.Language, item => item.Name, StringComparer.Ordinal),
            PlatformDiscountTermsItem.From(promotion.Terms),
            promotion.ValidFromUtc,
            promotion.ValidToUtc,
            promotion.Plans.Select(item => item.PlatformPlanId).ToArray(),
            planNames,
            promotion.Term,
            promotion.AtRegistration,
            promotion.ByStaff,
            promotion.RedemptionLimit,
            promotion.Redemptions,
            promotion.Status,
            names.TryGetValue(promotion.DesignedByUserId, out string? designer) ? designer : null,
            promotion.DecidedByUserId is Guid decider && names.TryGetValue(decider, out string? decided) ? decided : null,
            promotion.DecidedUtc,
            promotion.DecisionReason,
            promotion.CreatedUtc);
        bool approve = await _sales.HoldsAsync(context, PlatformPermissions.PromotionsApprove, cancellationToken);

        return GetPlatformPromotionResponse.Success(
            detail,
            new PlatformSalesAbilities(
                await _sales.HoldsAsync(context, PlatformPermissions.PromotionsManage, cancellationToken),
                approve && promotion.Status == PlatformPromotionStatus.PendingApproval && promotion.SubmittedByUserId != context.PlatformUserId));
    }
}
