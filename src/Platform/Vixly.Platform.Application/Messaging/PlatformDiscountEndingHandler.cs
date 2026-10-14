using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformDiscountEndingHandler.cs
//
// PURPOSE:
// Tells the tenant's billing contact that a discount ends (D-S25).
//
// RESPONSIBILITIES:
// - Email the billing contact, in the tenant language, with the date and the next full amount
//
// IMPORTANT NOTES:
// - The billing contact falls back to the owner (Slice 2)
// - Nothing is sent for a tenant without contacts
// =========================================================
public sealed class PlatformDiscountEndingHandler : IPlatformOutboxHandler
{
    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformSalesMailer _mailer;

    public PlatformDiscountEndingHandler(
        IPlatformSubscriptionRepository subscriptions,
        IPlatformPlanRepository plans,
        IPlatformTenantRepository tenants,
        IPlatformSalesMailer mailer)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.DiscountEnding;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformDiscountEnding ending = PlatformOutboxWriter.Read<PlatformDiscountEnding>(message);
        PlatformSubscription? subscription = await _subscriptions.GetAsync(ending.PlatformSubscriptionId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformTenant? tenant = await _tenants.GetAsync(ending.PlatformTenantId, cancellationToken);
        PlatformPlanVersion? version = plan?.Current ?? plan?.Versions.FirstOrDefault(item => item.Id == subscription!.PlanVersionId);

        if (subscription is null || plan is null || version is null || tenant is null || tenant.Contacts.Count == 0 || !version.Offers(subscription.Term))
        {
            return;
        }

        await _mailer.SendDiscountEndingAsync(
            tenant,
            tenant.ContactFor(PlatformTenantContactRole.Billing),
            plan.NameIn(tenant.DefaultLanguage),
            ending.EndsUtc,
            PlatformPricing.PeriodAmount(version, subscription.Term, subscription.Licences),
            version.CurrencyCode,
            cancellationToken);
    }
}
