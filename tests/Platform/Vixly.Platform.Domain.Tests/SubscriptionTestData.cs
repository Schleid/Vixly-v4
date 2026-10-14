using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: SubscriptionTestData.cs
//
// PURPOSE:
// Builders for plan and subscription tests.
//
// RESPONSIBILITIES:
// - Create published plans with chosen prices, limits and trial
// - Create subscriptions in a given status at a fixed time
// - Assert domain error codes
//
// IMPORTANT NOTES:
// - Times are fixed UTC values; tests never read the clock
// =========================================================
internal static class SubscriptionTestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid Actor = Guid.Parse("0199e3a0-0000-7000-8000-000000000001");

    public static readonly Guid Tenant = Guid.Parse("0199e3a0-0000-7000-8000-0000000000aa");

    public static PlatformPlanVersionTerms Terms(
        decimal monthly = 10m,
        decimal annual = 100m,
        decimal setup = 50m,
        int minimum = 2,
        int? trialDays = 30,
        int maxDevices = 10,
        int maxWarehouses = 2,
        string currency = "USD",
        params PlatformPlanFeature[] features)
    {
        return PlatformPlanVersionTerms.Create(currency, monthly, annual, setup, minimum, trialDays, maxDevices, maxWarehouses, features);
    }

    public static PlatformPlan Published(string code = "BASIC", PlatformPlanVersionTerms? terms = null)
    {
        PlatformPlan plan = PlatformPlan.Create(code, code + " plan", "Description", "USD", Actor, Now.AddDays(-30));
        plan.EditDraft(terms ?? Terms(), Now.AddDays(-30));
        plan.PublishDraft(Actor, Now.AddDays(-30));

        return plan;
    }

    public static PlatformSubscription Started(PlatformPlan plan, bool trial = false, int licences = 3, PlatformSubscriptionTerm term = PlatformSubscriptionTerm.Monthly)
    {
        return PlatformSubscription.Start(Tenant, plan, term, licences, 10, trial, Actor, Now).Subscription;
    }

    public static PlatformSubscription Active(PlatformPlan plan, int licences = 3, PlatformSubscriptionTerm term = PlatformSubscriptionTerm.Monthly)
    {
        PlatformSubscription subscription = Started(plan, false, licences, term);
        subscription.RecordOfflinePayment("Bank transfer", Actor, Now);

        return subscription;
    }

    public static void AssertCode(string code, Action act)
    {
        DomainException refused = Assert.Throws<DomainException>(act);
        Assert.Equal(code, refused.Error.Code);
    }
}
