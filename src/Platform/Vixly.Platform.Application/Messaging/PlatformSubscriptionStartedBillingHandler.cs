using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSubscriptionStartedBillingHandler.cs
//
// PURPOSE:
// Invoices the first period when a subscription starts waiting for payment.
//
// RESPONSIBILITIES:
// - Read the subscription message and decide what is due
// - Add pending one-time charges to first-period and renewal invoices and mark them billed
// - Issue the invoice in its own commit; one invoice per subscription event
//
// IMPORTANT NOTES:
// - Idempotent: the event's source key is unique, so a replayed message bills nothing twice
// - A failed commit is retried by the outbox; one tenant failing never stops another
// - Nothing is invoiced when the total is zero (Slice 3 activates at once, D-S25)
// =========================================================
public sealed class PlatformSubscriptionStartedBillingHandler : PlatformSubscriptionBillingHandler
{
    public PlatformSubscriptionStartedBillingHandler(
        IPlatformSubscriptionRepository subscriptions,
        IPlatformPlanRepository plans,
        IPlatformTenantRepository tenants,
        PlatformBillingAdministration billing,
        PlatformInvoiceIssuer issuer,
        IPlatformUnitOfWork unitOfWork)
        : base(subscriptions, plans, tenants, billing, issuer, unitOfWork)
    {
    }

    public override string MessageType => PlatformMessageTypes.SubscriptionStarted;
}
