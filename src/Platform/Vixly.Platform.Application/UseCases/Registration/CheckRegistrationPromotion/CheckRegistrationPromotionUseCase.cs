using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Registration.CheckRegistrationPromotion;

// =========================================================
// FILE: CheckRegistrationPromotionUseCase.cs
//
// PURPOSE:
// Checks a promotion code at the plan step of registration (D-S20).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous; Platform.Api rate-limits it
// - Unapproved promotions answer "not found", never revealing that they exist
// - Off while the Sales setting does not allow codes at registration
// =========================================================
public sealed class CheckRegistrationPromotionUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformPlanRepository _plans;

    public CheckRegistrationPromotionUseCase(
        PlatformSalesAdministration sales,
        IPlatformPlanRepository plans)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task<CheckRegistrationPromotionResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CheckRegistrationPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _sales.UtcNow;
        PlatformSalesSettings settings = await _sales.SettingsAsync(cancellationToken);

        if (!settings.PromotionCodesAtRegistration)
        {
            return CheckRegistrationPromotionResponse.Failure(PlatformSalesErrors.PromotionNotAtRegistration);
        }

        PlatformPromotion? promotion = string.IsNullOrWhiteSpace(request.Code)
            ? null
            : await _sales.Promotions.GetByCodeAsync(request.Code.Trim().ToUpperInvariant(), cancellationToken);
        PlatformPlan? plan = string.IsNullOrWhiteSpace(request.PlanCode)
            ? null
            : await _plans.GetByCodeAsync(request.PlanCode.Trim().ToUpperInvariant(), cancellationToken);

        if (promotion is null || promotion.Status is PlatformPromotionStatus.Draft or PlatformPromotionStatus.PendingApproval or PlatformPromotionStatus.Rejected)
        {
            return CheckRegistrationPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        if (plan is null || !plan.IsPublic || !plan.CanBeChosen)
        {
            return CheckRegistrationPromotionResponse.Failure(PlatformSubscriptionErrors.PlanNotAvailable);
        }

        if (promotion.Refusal(plan.Id, request.Term, atRegistration: true, utcNow) is { } refused)
        {
            return CheckRegistrationPromotionResponse.Failure(refused);
        }

        return CheckRegistrationPromotionResponse.Success(promotion.NameIn(PlatformLanguageInput.Read(request.Language)), PlatformDiscountTermsItem.From(promotion.Terms));
    }
}
