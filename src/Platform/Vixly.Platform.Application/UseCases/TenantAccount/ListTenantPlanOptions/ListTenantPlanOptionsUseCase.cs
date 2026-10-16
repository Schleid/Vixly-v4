using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantPlanOptions;

// =========================================================
// FILE: ListTenantPlanOptionsUseCase.cs
//
// PURPOSE:
// Plans the tenant may move to by itself: public plans on sale and its current plan (call 20).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role
// =========================================================
public sealed class ListTenantPlanOptionsUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    public ListTenantPlanOptionsUseCase(
        PlatformSubscriptionAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<ListTenantPlanOptionsResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ListTenantPlanOptionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        List<PlatformPlanOption> options = (await _administration.PlanOptionsAsync(context.PlatformTenantId, language, cancellationToken, publicOnly: true)).ToList();
        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(context.PlatformTenantId, cancellationToken);
        PlatformPlan? current = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);

        if (current?.Current is not null && options.All(option => option.PlatformPlanId != current.Id))
        {
            options.Insert(0, await _administration.OptionAsync(context.PlatformTenantId, current, language, cancellationToken));
        }

        return ListTenantPlanOptionsResponse.Success(options);
    }
}
