using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.TenantAccount.PreviewTenantSubscriptionChange;

// =========================================================
// FILE: PreviewTenantSubscriptionChangeUseCase.cs
//
// PURPOSE:
// What a change of plan, term or licences would cost the tenant, before it confirms (call 21).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role; nothing is saved
// - Same rules as staff changes (Slice 3) but only public plans or the current plan
// =========================================================
public sealed class PreviewTenantSubscriptionChangeUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    public PreviewTenantSubscriptionChangeUseCase(
        PlatformSubscriptionAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<PreviewTenantSubscriptionChangeResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        PreviewTenantSubscriptionChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(context.PlatformTenantId, cancellationToken);
        PlatformPlan? current = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);

        if (subscription is null || current is null)
        {
            return PreviewTenantSubscriptionChangeResponse.Failure(PlatformSubscriptionErrors.NoSubscription);
        }

        PlatformPlan? target = await PlatformTenantPlanChoice.TargetAsync(_administration, current, request.PlatformPlanId, cancellationToken);

        if (target is null)
        {
            return PreviewTenantSubscriptionChangeResponse.Failure(PlatformTenantAccountErrors.PlanNotOffered);
        }

        int maxLicences = await _administration.MaxLicencesAsync(context.PlatformTenantId, PlatformTenantPlanChoice.Ceiling(subscription, target), cancellationToken);
        int activeLicences = await _administration.ActiveLicencesAsync(context.PlatformTenantId, cancellationToken);
        PlatformSubscriptionQuote quote;

        try
        {
            quote = subscription.Quote(current, target, request.Term, request.Licences, maxLicences, activeLicences, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return PreviewTenantSubscriptionChangeResponse.Failure(exception.Error);
        }

        return PreviewTenantSubscriptionChangeResponse.Success(PlatformSubscriptionQuoteItem.From(quote));
    }
}
