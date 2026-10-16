using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ChangeTenantSubscription;

// =========================================================
// FILE: ChangeTenantSubscriptionUseCase.cs
//
// PURPOSE:
// The tenant changes its plan, term or licences by itself (call 21).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Owner or admin with a second factor checked
// - Audited with the tenant actor; billing and emails follow as for a staff change (Slice 3)
// =========================================================
public sealed class ChangeTenantSubscriptionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ChangeTenantSubscriptionUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeTenantSubscriptionResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ChangeTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner, PlatformTenantActorRole.Admin);

        if (notAllowed is not null)
        {
            return ChangeTenantSubscriptionResponse.Failure(notAllowed);
        }

        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(context.PlatformTenantId, cancellationToken);
        PlatformPlan? current = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);

        if (subscription is null || current is null)
        {
            return ChangeTenantSubscriptionResponse.Failure(PlatformSubscriptionErrors.NoSubscription);
        }

        PlatformPlan? target = await PlatformTenantPlanChoice.TargetAsync(_administration, current, request.PlatformPlanId, cancellationToken);

        if (target is null)
        {
            return ChangeTenantSubscriptionResponse.Failure(PlatformTenantAccountErrors.PlanNotOffered);
        }

        int maxLicences = await _administration.MaxLicencesAsync(context.PlatformTenantId, PlatformTenantPlanChoice.Ceiling(subscription, target), cancellationToken);
        int activeLicences = await _administration.ActiveLicencesAsync(context.PlatformTenantId, cancellationToken);
        PlatformSubscriptionQuote quote;
        PlatformSubscriptionEvent change;

        try
        {
            quote = subscription.Quote(current, target, request.Term, request.Licences, maxLicences, activeLicences, _administration.UtcNow);
            change = subscription.Apply(quote, current.Code, null, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ChangeTenantSubscriptionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(
            null,
            subscription,
            target.Id == subscription.PlatformPlanId ? target : current,
            change,
            null,
            quote.Timing == PlatformSubscriptionChangeTiming.Now ? "platform.subscriptions.changed" : "platform.subscriptions.change_scheduled",
            quote.ChargeNow,
            context);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeTenantSubscriptionResponse.Success(PlatformSubscriptionQuoteItem.From(quote))
            : ChangeTenantSubscriptionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
