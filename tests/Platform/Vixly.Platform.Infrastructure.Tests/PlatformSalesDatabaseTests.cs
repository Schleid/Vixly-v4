using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
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
// FILE: PlatformSalesDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 3b mapping and guarantees against SQL Server.
//
// RESPONSIBILITIES:
// - Subscriptions round-trip with discounts, charges and the setup fee override
// - Sales settings, promotions and offers round-trip
// - The approval, promotion and offer queries
//
// IMPORTANT NOTES:
// - Runs only when VIXLY_TEST_SQL_SERVER is set (SqlServerFact)
// =========================================================
public sealed class PlatformSalesDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformSalesDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Subscription_RoundTrips_WithDiscountsChargesAndSetupFee()
    {
        PlatformPlan plan = NewPlan();
        PlatformTenant tenant = NewTenant();
        PlatformSubscription subscription = PlatformSubscription.Start(tenant.Id, plan, PlatformSubscriptionTerm.Monthly, 3, 10, false, null, Now).Subscription;
        subscription.ChangeSetupFee(0m, plan.Current!, "Waived", _database.ActorUserId, Now);
        subscription.RequestDiscount(Percent(100m, 1), PlatformDiscountSource.Promotion, Guid.NewGuid(), "LAUNCH", false, null, Now);
        subscription.RequestDiscount(Percent(30m, 6), PlatformDiscountSource.Staff, null, "Valued customer", true, null, Now);
        subscription.AddCharge("Data migration", 1200m, "USD", "Contract", _database.ActorUserId, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.Add(plan);
            context.Tenants.Add(tenant);
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformSubscription loaded = (await new PlatformSubscriptionRepository(context).GetAsync(subscription.Id, CancellationToken.None))!;

            Assert.Equal(0m, loaded.SetupFeeOverride);
            Assert.Equal(100m, loaded.ActiveDiscount!.Terms.Value);
            Assert.Equal(PlatformDiscountDuration.Periods, loaded.ActiveDiscount.Terms.Duration);
            Assert.Equal(30m, loaded.PendingDiscount!.Terms.Value);
            Assert.Equal(1200m, Assert.Single(loaded.Charges).Amount);

            loaded.ApproveDiscount(loaded.PendingDiscount.Id, "Agreed", _database.ActorUserId, Now.AddMinutes(1));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformSubscriptionRepository repository = new(check);
        PlatformSubscription final = (await repository.GetAsync(subscription.Id, CancellationToken.None))!;

        Assert.Equal(30m, final.ActiveDiscount!.Terms.Value);
        Assert.Equal(PlatformDiscountEndReason.Replaced, final.Discounts.Single(item => item.Terms.Value == 100m).EndReason);
        Assert.True(await repository.HasUsedPromotionAsync(tenant.Id, final.Discounts.Single(item => item.Source == PlatformDiscountSource.Promotion).SourceId!.Value, CancellationToken.None));
        Assert.Contains(await repository.ListWithUnnoticedDiscountsAsync(50, CancellationToken.None), item => item.Id == subscription.Id);
    }

    [SqlServerFact]
    public async Task OneActiveDiscount_HoldsWhenTwoGrantsRace()
    {
        PlatformPlan plan = NewPlan();
        PlatformTenant tenant = NewTenant();

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.Add(plan);
            context.Tenants.Add(tenant);
            context.Subscriptions.Add(PlatformSubscription.Start(tenant.Id, plan, PlatformSubscriptionTerm.Monthly, 3, 10, false, null, Now).Subscription);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext one = _database.NewContext();
        await using PlatformDbContext two = _database.NewContext();
        PlatformSubscription first = (await new PlatformSubscriptionRepository(one).GetCurrentAsync(tenant.Id, CancellationToken.None))!;
        PlatformSubscription second = (await new PlatformSubscriptionRepository(two).GetCurrentAsync(tenant.Id, CancellationToken.None))!;
        first.RequestDiscount(Percent(10m, 2), PlatformDiscountSource.Staff, null, "One", false, null, Now);
        second.RequestDiscount(Percent(20m, 2), PlatformDiscountSource.Staff, null, "Two", false, null, Now);

        Assert.True(await new PlatformUnitOfWork(one).SaveChangesAsync(CancellationToken.None));
        Assert.False(await new PlatformUnitOfWork(two).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task SalesData_RoundTrips_AndTheQueriesListWhatWaits()
    {
        PlatformPlan plan = NewPlan();
        plan.StartDraft(Now);
        plan.SubmitDraft(_database.ActorUserId, Now);
        PlatformPromotion promotion = PlatformPromotion.Create(
            "PR" + Guid.NewGuid().ToString("N")[..8], "Launch", Percent(100m, 1), Now, Now.AddMonths(1), new[] { plan.Id }, PlatformSubscriptionTerm.Monthly, true, false, 50, _database.ActorUserId, Now);
        promotion.SetName(LanguageCode.Fr, "Lancement", Now);
        promotion.Submit(_database.ActorUserId, Now);
        PlatformOffer offer = PlatformOffer.Prepare(
            "Acme " + Guid.NewGuid().ToString("N")[..6], "buyer@acme.example", "Partnership", plan.Id, PlatformSubscriptionTerm.Annual, 5,
            Percent(35m, 3), 0m, new[] { ("Training", 500m) }, false, Now.AddDays(10), 1, _database.ActorUserId, Now);
        offer.Submit(true, null, Now);
        PlatformOffer issued = PlatformOffer.Prepare(
            "Issued " + Guid.NewGuid().ToString("N")[..6], null, "Quick deal", plan.Id, PlatformSubscriptionTerm.Monthly, 3,
            null, null, Array.Empty<(string, decimal)>(), false, Now.AddDays(10), 2, _database.ActorUserId, Now);
        string hash = "hash-" + Guid.NewGuid().ToString("N");
        issued.Submit(false, hash, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Plans.Add(plan);
            context.Promotions.Add(promotion);
            context.Offers.Add(offer);
            context.Offers.Add(issued);
            PlatformSalesSettings settings = await new PlatformSalesSettingsRepository(context).GetAsync(CancellationToken.None) ?? PlatformSalesSettings.Empty(Now);

            if (context.Entry(settings).State == EntityState.Detached)
            {
                context.SalesSettings.Add(settings);
            }

            settings.Update(20m, new Dictionary<string, decimal> { ["USD"] = 100m, ["EUR"] = 90m }, false, 14, 30, true, _database.ActorUserId, Now);
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformSalesSettings loadedSettings = (await new PlatformSalesSettingsRepository(check).GetAsync(CancellationToken.None))!;
        PlatformPromotion loadedPromotion = (await new PlatformPromotionRepository(check).GetByCodeAsync(promotion.Code, CancellationToken.None))!;
        PlatformOffer loadedOffer = (await new PlatformOfferRepository(check).GetAsync(offer.Id, CancellationToken.None))!;
        PlatformOffer byHash = (await new PlatformOfferRepository(check).GetByTokenHashAsync(hash, CancellationToken.None))!;
        PlatformSubscriptionQueries queries = new(check);
        IReadOnlyList<PlatformApprovalItem> approvals = await queries.ListApprovalsAsync(
            new[] { PlatformApprovalKind.Offer, PlatformApprovalKind.Promotion, PlatformApprovalKind.PlanVersion }, "fr", CancellationToken.None);
        PagedResult<PlatformPromotionListItem> promotions = await queries.ListPromotionsAsync(new PlatformPromotionListFilter(null, promotion.Code, "fr"), PagedRequest.First, CancellationToken.None);
        PagedResult<PlatformOfferListItem> offers = await queries.ListOffersAsync(new PlatformOfferListFilter(PlatformOfferStatus.PendingApproval, offer.CompanyName, "en"), PagedRequest.First, CancellationToken.None);

        Assert.Equal(2, loadedSettings.FixedLimits.Count);
        Assert.False(loadedSettings.DiscountNeedsApproval(Percent(20m, 1), "USD"));
        Assert.Equal("Lancement", loadedPromotion.NameIn(LanguageCode.Fr));
        Assert.Equal(plan.Id, Assert.Single(loadedPromotion.Plans).PlatformPlanId);
        Assert.Equal(35m, loadedOffer.Discount!.Value);
        Assert.Equal("Training", Assert.Single(loadedOffer.Charges).Description);
        Assert.Null(byHash.Discount);
        Assert.Contains(approvals, item => item.Kind == PlatformApprovalKind.Offer && item.TargetId == offer.Id);
        Assert.Contains(approvals, item => item.Kind == PlatformApprovalKind.Promotion && item.Detail == "Lancement");
        Assert.Contains(approvals, item => item.Kind == PlatformApprovalKind.PlanVersion && item.TargetId == plan.Id && item.RequestedByName is not null);
        Assert.Equal("Lancement", Assert.Single(promotions.Items).Name);
        Assert.Equal(35m, Assert.Single(offers.Items).Value);
    }

    private static PlatformDiscountTerms Percent(decimal value, int periods)
    {
        return PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, value, PlatformDiscountDuration.Periods, periods, null, Now);
    }

    private PlatformPlan NewPlan()
    {
        PlatformPlan plan = PlatformPlan.Create("S" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), "Growth", null, "USD", _database.ActorUserId, Now);
        plan.EditDraft(PlatformPlanVersionTerms.Create("USD", 10m, 100m, 50m, 2, 30, 10, 2, new[] { PlatformPlanFeature.DataExport }), Now);
        plan.PublishDraft(_database.ActorUserId, Now);

        return plan;
    }

    private PlatformTenant NewTenant()
    {
        string slug = "sale-" + Guid.NewGuid().ToString("N")[..8];

        return PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            slug,
            PlatformCompanyDetails.Create(
                "Acme Freight Inc.",
                "Acme",
                EmailAddress.Create("office@acme.example"),
                PhoneNumber.Create("+1 514 555 0100"),
                PlatformPostalAddress.Create("1 Harbour Road", null, "Montreal", "QC", "H2X 1Y4", "CA")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@acme.example"), null),
            LanguageCode.En,
            "America/Toronto",
            _database.ActorUserId,
            Now).Tenant;
    }
}
