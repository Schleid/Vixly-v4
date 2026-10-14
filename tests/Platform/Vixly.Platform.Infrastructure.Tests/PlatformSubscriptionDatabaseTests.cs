using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformSubscriptionDatabaseTests.cs
//
// PURPOSE:
// Proves the subscription mapping and guarantees against SQL Server (Slice 3).
//
// RESPONSIBILITIES:
// - Plans round-trip with translations, versions, limits and features
// - A tenant can never hold two current subscriptions (D-S3)
// - A subscription is renewed once even when two hosts race
// - The queries list plans, subscriptions and history
//
// IMPORTANT NOTES:
// - Runs only when VIXLY_TEST_SQL_SERVER is set (SqlServerFact)
// =========================================================
public sealed class PlatformSubscriptionDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformSubscriptionDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Plan_RoundTrips_WithTranslationsVersionsLimitsAndFeatures()
    {
        PlatformPlan plan = NewPlan();
        plan.SetTranslation(LanguageCode.Fr, "Croissance", "Pour grandir", Now);
        plan.StartDraft(Now);
        plan.EditDraft(Terms(12m), Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.Add(plan);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformPlan loaded = (await new PlatformPlanRepository(context).GetAsync(plan.Id, CancellationToken.None))!;
            Assert.Equal("Croissance", loaded.NameIn(LanguageCode.Fr));
            Assert.Equal(10m, loaded.Current!.MonthlyPricePerLicence);
            Assert.Equal(12m, loaded.Draft!.MonthlyPricePerLicence);
            Assert.Equal(10, loaded.Current.Limit(PlatformPlanLimitKind.MaxLicensedDevices));
            Assert.True(loaded.Current.Includes(PlatformPlanFeature.DataExport));

            loaded.PublishDraft(_database.ActorUserId, Now.AddMinutes(1));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformPlan loaded = (await new PlatformPlanRepository(context).GetAsync(plan.Id, CancellationToken.None))!;
            Assert.Equal(2, loaded.Current!.Number);
            Assert.Equal(PlatformPlanVersionStatus.Superseded, loaded.Versions.Single(version => version.Number == 1).Status);

            loaded.StartDraft(Now.AddMinutes(2));
            await context.SaveChangesAsync();
            loaded.DiscardDraft(Now.AddMinutes(3));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext check = _database.NewContext();
        Assert.Equal(2, await check.Set<PlatformPlanVersion>().CountAsync(version => version.PlatformPlanId == plan.Id));
    }

    [SqlServerFact]
    public async Task ATenant_NeverHoldsTwoCurrentSubscriptions()
    {
        PlatformPlan plan = NewPlan();
        PlatformTenant tenant = NewTenant();

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.Add(plan);
            context.Tenants.Add(tenant);
            context.Subscriptions.Add(Start(tenant, plan));
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext second = _database.NewContext())
        {
            second.Subscriptions.Add(Start(tenant, (await new PlatformPlanRepository(second).GetAsync(plan.Id, CancellationToken.None))!));
            Assert.False(await new PlatformUnitOfWork(second).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext ending = _database.NewContext())
        {
            PlatformSubscription current = (await new PlatformSubscriptionRepository(ending).GetCurrentAsync(tenant.Id, CancellationToken.None))!;
            current.EndNow("Moving to a new plan", _database.ActorUserId, Now);
            await ending.SaveChangesAsync();
        }

        await using PlatformDbContext again = _database.NewContext();
        again.Subscriptions.Add(Start(tenant, (await new PlatformPlanRepository(again).GetAsync(plan.Id, CancellationToken.None))!));
        Assert.True(await new PlatformUnitOfWork(again).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Renewal_IsSavedOnce_WhenTwoHostsRace()
    {
        PlatformPlan plan = NewPlan();
        PlatformTenant tenant = NewTenant();
        PlatformSubscription subscription = Start(tenant, plan);
        subscription.RecordOfflinePayment("Wire", _database.ActorUserId, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.Add(plan);
            context.Tenants.Add(tenant);
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
        }

        DateTimeOffset later = Now.AddMonths(1).AddHours(1);
        await using PlatformDbContext one = _database.NewContext();
        await using PlatformDbContext two = _database.NewContext();
        Assert.Contains(subscription.Id, await new PlatformSubscriptionRepository(one).ListDueAsync(later, 1000, CancellationToken.None));

        PlatformSubscription first = (await new PlatformSubscriptionRepository(one).GetAsync(subscription.Id, CancellationToken.None))!;
        PlatformSubscription second = (await new PlatformSubscriptionRepository(two).GetAsync(subscription.Id, CancellationToken.None))!;
        PlatformPlan planOne = (await new PlatformPlanRepository(one).GetAsync(plan.Id, CancellationToken.None))!;
        PlatformPlan planTwo = (await new PlatformPlanRepository(two).GetAsync(plan.Id, CancellationToken.None))!;
        one.SubscriptionEvents.Add(first.RenewIfDue(planOne, null, later)!.Event);
        two.SubscriptionEvents.Add(second.RenewIfDue(planTwo, null, later)!.Event);

        Assert.True(await new PlatformUnitOfWork(one).SaveChangesAsync(CancellationToken.None));
        Assert.False(await new PlatformUnitOfWork(two).SaveChangesAsync(CancellationToken.None));

        await using PlatformDbContext check = _database.NewContext();
        Assert.Equal(1, await check.SubscriptionEvents.CountAsync(item => item.PlatformSubscriptionId == subscription.Id && item.EventCode == PlatformSubscriptionEventCodes.Renewed));
    }

    [SqlServerFact]
    public async Task Queries_ListPlansSubscriptionsAndHistory_WithAScheduledChange()
    {
        PlatformPlan basic = NewPlan();
        PlatformPlan growth = NewPlan(20m);
        PlatformTenant tenant = NewTenant();
        PlatformSubscriptionStart start = PlatformSubscription.Start(tenant.Id, growth, PlatformSubscriptionTerm.Monthly, 4, 10, false, _database.ActorUserId, Now);
        PlatformSubscription subscription = start.Subscription;
        PlatformSubscriptionEvent paid = subscription.RecordOfflinePayment("Wire", _database.ActorUserId, Now);
        PlatformSubscriptionEvent scheduled = subscription.Apply(
            subscription.Quote(growth, basic, PlatformSubscriptionTerm.Monthly, 3, 10, 0, Now.AddDays(10)),
            growth.Code,
            _database.ActorUserId,
            Now.AddDays(10));

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.AddRange(basic, growth);
            context.Tenants.Add(tenant);
            context.Subscriptions.Add(subscription);
            context.SubscriptionEvents.AddRange(start.Event, paid, scheduled);
            context.TenantLimitOverrides.Add(PlatformTenantLimitOverride.Set(tenant.Id, PlatformPlanLimitKind.MaxWarehouses, 5, "Peak", null, _database.ActorUserId, Now));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext read = _database.NewContext();
        PlatformSubscriptionQueries queries = new(read);

        PagedResult<PlatformPlanListItem> plans = await queries.ListPlansAsync(new PlatformPlanListFilter(null, growth.Code, "fr"), PagedRequest.First, CancellationToken.None);
        PlatformPlanListItem listed = Assert.Single(plans.Items);
        Assert.Equal((1, 1, 20m), (listed.CurrentVersionNumber!.Value, listed.Subscribers, listed.MonthlyPricePerLicence!.Value));

        PagedResult<PlatformSubscriptionListItem> subscriptions = await queries.ListSubscriptionsAsync(
            new PlatformSubscriptionListFilter(null, growth.Id, null, null, false, false, "en"), PagedRequest.First, CancellationToken.None);
        PlatformSubscriptionListItem row = Assert.Single(subscriptions.Items);
        Assert.Equal((80m, true, tenant.TenantCode), (row.PeriodAmount, row.HasScheduledChange, row.TenantCode));

        PlatformSubscription loaded = (await new PlatformSubscriptionRepository(read).GetCurrentAsync(tenant.Id, CancellationToken.None))!;
        Assert.Equal((basic.Id, 3), (loaded.ScheduledChange!.TargetPlanId, loaded.ScheduledChange.TargetLicences));

        PagedResult<PlatformSubscriptionEventItem> history = await queries.ListEventsAsync(tenant.Id, PagedRequest.First, CancellationToken.None);
        Assert.Equal(PlatformSubscriptionEventCodes.ChangeScheduled, history.Items[0].EventCode);
        Assert.Equal(3, history.TotalCount);

        IReadOnlyList<PlatformTenantLimitOverride> open = await new PlatformLimitOverrideRepository(read).ListOpenAsync(tenant.Id, CancellationToken.None);
        Assert.Equal(5, Assert.Single(open).Value);
    }

    private PlatformPlan NewPlan(decimal monthly = 10m)
    {
        PlatformPlan plan = PlatformPlan.Create("P" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), "Growth", null, "USD", _database.ActorUserId, Now);
        plan.EditDraft(Terms(monthly), Now);
        plan.PublishDraft(_database.ActorUserId, Now);

        return plan;
    }

    private static PlatformPlanVersionTerms Terms(decimal monthly)
    {
        return PlatformPlanVersionTerms.Create("USD", monthly, monthly * 10, 50m, 2, 30, 10, 2, new[] { PlatformPlanFeature.DataExport });
    }

    private static PlatformSubscription Start(PlatformTenant tenant, PlatformPlan plan)
    {
        return PlatformSubscription.Start(tenant.Id, plan, PlatformSubscriptionTerm.Monthly, 3, 10, false, null, Now).Subscription;
    }

    private PlatformTenant NewTenant()
    {
        string slug = "sub-" + Guid.NewGuid().ToString("N")[..8];

        return PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            slug,
            PlatformCompanyDetails.Create(
                "Northwind Freight Inc.",
                "Northwind",
                EmailAddress.Create("office@northwind.example"),
                PhoneNumber.Create("+1 514 555 0100"),
                PlatformPostalAddress.Create("1 Harbour Road", null, "Montreal", "QC", "H2X 1Y4", "CA")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Grace Hopper", EmailAddress.Create("owner@northwind.example"), null),
            LanguageCode.Fr,
            "America/Toronto",
            _database.ActorUserId,
            Now).Tenant;
    }
}
