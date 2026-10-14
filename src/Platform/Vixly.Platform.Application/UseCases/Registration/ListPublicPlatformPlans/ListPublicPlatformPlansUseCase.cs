using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Registration.ListPublicPlatformPlans;

// =========================================================
// FILE: ListPublicPlatformPlansUseCase.cs
//
// PURPOSE:
// Lists the public plans for the registration catalogue.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous (Platform.Api, rate-limited and cached); never shows retired, private or unpublished plans
// =========================================================
public sealed class ListPublicPlatformPlansUseCase
{
    private readonly IPlatformPlanRepository _plans;

    public ListPublicPlatformPlansUseCase(
        IPlatformPlanRepository plans)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task<ListPublicPlatformPlansResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        ListPublicPlatformPlansRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        IReadOnlyList<PlatformPlan> plans = await _plans.ListChoosableAsync(publicOnly: true, cancellationToken);

        return ListPublicPlatformPlansResponse.Success(plans
            .Select(plan => new PlatformPublicPlanItem(
                plan.Code,
                plan.NameIn(language),
                plan.DescriptionIn(language),
                plan.Current!.CurrencyCode,
                plan.Current.MonthlyPricePerLicence,
                plan.Current.AnnualPricePerLicence,
                plan.Current.SetupFee,
                plan.Current.MinimumLicences,
                plan.Current.Limit(PlatformPlanLimitKind.MaxLicensedDevices),
                plan.Current.Limit(PlatformPlanLimitKind.MaxWarehouses),
                plan.Current.Features.Select(item => item.Feature).OrderBy(feature => feature).ToArray(),
                plan.Current.TrialDays))
            .ToArray());
    }
}
