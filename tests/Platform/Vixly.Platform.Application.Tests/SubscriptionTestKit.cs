using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: SubscriptionTestKit.cs
//
// PURPOSE:
// Builds plan and subscription use cases over the fakes.
//
// RESPONSIBILITIES:
// - Share the tenant kit (staff, tenants, clock, audit, outbox)
// - Create published plans and administration helpers
//
// IMPORTANT NOTES:
// - Times come from the fixed clock
// =========================================================
internal sealed class SubscriptionTestKit
{
    public TenantTestKit Tenants { get; } = new();

    public FakeSubscriptionStore Store { get; } = new();

    public FakeSalesStore Sales { get; } = new();

    public PlatformSalesAdministration SalesAdministration()
    {
        return new PlatformSalesAdministration(
            Tenants.Gate(),
            new PlatformStepUpGuard(Tenants.Store, Tenants.Clock),
            Tenants.Store,
            Sales,
            Sales,
            Sales,
            Tenants.Crypto,
            Sales,
            Tenants.Audit(),
            Tenants.Outbox(),
            Tenants.Clock);
    }

    public DateTimeOffset Now => Tenants.Clock.GetUtcNow();

    public PlatformPlanAdministration Plans()
    {
        return new PlatformPlanAdministration(
            Tenants.Gate(),
            new PlatformStepUpGuard(Tenants.Store, Tenants.Clock),
            Tenants.Store,
            Store,
            Tenants.Audit(),
            Tenants.Clock);
    }

    public PlatformSubscriptionAdministration Subscriptions()
    {
        return new PlatformSubscriptionAdministration(
            Tenants.Gate(),
            new PlatformStepUpGuard(Tenants.Store, Tenants.Clock),
            Tenants.Store,
            Store,
            Store,
            Store,
            Store,
            Tenants.Audit(),
            Tenants.Outbox(),
            Store,
            Tenants.Clock);
    }

    public PlatformPlan Plan(string code = "BASIC", decimal monthly = 10m, int? trialDays = 30, bool isPublic = true, int maxDevices = 10)
    {
        PlatformPlan plan = PlatformPlan.Create(code, code + " plan", null, "USD", Guid.NewGuid(), Now.AddDays(-60));
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", monthly, monthly * 10, 50m, 2, trialDays, maxDevices, 2, new[] { PlatformPlanFeature.DataExport }), Now.AddDays(-60));
        plan.PublishDraft(Guid.NewGuid(), Now.AddDays(-60));
        plan.SetCatalogue(isPublic, Store.Plans.Count, Now.AddDays(-60));
        Store.Plans.Add(plan);

        return plan;
    }

    public PlatformSubscription Subscribe(Guid tenantId, PlatformPlan plan, bool paid = true, int licences = 3)
    {
        PlatformSubscription subscription = PlatformSubscription.Start(tenantId, plan, PlatformSubscriptionTerm.Monthly, licences, 10, false, null, Now.AddDays(-15)).Subscription;

        if (paid)
        {
            subscription.RecordOfflinePayment("Wire", Guid.NewGuid(), Now.AddDays(-15));
        }

        Store.Subscriptions.Add(subscription);

        return subscription;
    }

    public PlatformAuthorizationContext Staff(params string[] permissions)
    {
        return Tenants.Staff(permissions);
    }
}
