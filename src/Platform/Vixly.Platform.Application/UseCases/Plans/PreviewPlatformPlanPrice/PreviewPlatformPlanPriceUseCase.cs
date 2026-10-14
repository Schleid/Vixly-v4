using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Plans.PreviewPlatformPlanPrice;

// =========================================================
// FILE: PreviewPlatformPlanPriceUseCase.cs
//
// PURPOSE:
// Prices a period of a plan for a term and a number of licences (D-S9).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Uses the current version, or the draft before the first publication
// - Amounts before tax
// =========================================================
public sealed class PreviewPlatformPlanPriceUseCase
{
    private readonly PlatformPlanAdministration _administration;

    public PreviewPlatformPlanPriceUseCase(
        PlatformPlanAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<PreviewPlatformPlanPriceResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        PreviewPlatformPlanPriceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansView, request.PlatformPlanId, requireStepUp: false, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return PreviewPlatformPlanPriceResponse.Failure(target.Error!);
        }

        PlatformPlanVersion? version = plan.Current ?? plan.Draft;

        if (version is null || !Enum.IsDefined(request.Term) || request.Licences < 1 || request.Licences > PlatformSubscriptionLimits.MaxLicences)
        {
            return PreviewPlatformPlanPriceResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        if (!version.Offers(request.Term))
        {
            return PreviewPlatformPlanPriceResponse.Failure(PlatformSubscriptionErrors.TermNotOffered);
        }

        return PreviewPlatformPlanPriceResponse.Success(
            PlatformPricing.PeriodAmount(version, request.Term, request.Licences),
            version.SetupFee,
            version.CurrencyCode);
    }
}
