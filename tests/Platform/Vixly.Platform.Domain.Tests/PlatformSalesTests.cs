using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformSalesTests.cs
//
// PURPOSE:
// Proves Sales settings, promotions, offer links and plan approval (D-S20 to D-S26).
//
// RESPONSIBILITIES:
// - No commercial value preset: empty settings mean approval
// - Promotions and plan versions go live only after approval by someone else
// - Offer links expire, are used, tied to an email and revoked
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformSalesTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    private static readonly Guid Sales = SubscriptionTestData.Actor;

    private static readonly Guid Manager = Guid.Parse("0199e3a0-0000-7000-8000-000000000002");

    private static PlatformDiscountTerms Percent(decimal value, bool noEnd = false)
    {
        return PlatformDiscountTerms.Create(PlatformDiscountKind.Percent, value, noEnd ? PlatformDiscountDuration.NoEnd : PlatformDiscountDuration.Periods, noEnd ? null : 3, null, Now);
    }

    [Fact]
    public void EmptySettings_SendEverythingForApproval()
    {
        PlatformSalesSettings settings = PlatformSalesSettings.Empty(Now);

        Assert.True(settings.DiscountNeedsApproval(Percent(1m), "USD"));
        Assert.True(settings.OfferNeedsApproval(null, "USD", Now.AddDays(1), Now));
    }

    [Fact]
    public void Limits_DecideWhatNeedsApproval()
    {
        PlatformSalesSettings settings = PlatformSalesSettings.Empty(Now);
        settings.Update(20m, new Dictionary<string, decimal> { ["usd"] = 50m }, false, 14, 30, true, Manager, Now);
        PlatformDiscountTerms fixedForty = PlatformDiscountTerms.Create(PlatformDiscountKind.FixedAmount, 40m, PlatformDiscountDuration.Periods, 2, null, Now);

        Assert.False(settings.DiscountNeedsApproval(Percent(20m), "USD"));
        Assert.True(settings.DiscountNeedsApproval(Percent(20.01m), "USD"));
        Assert.True(settings.DiscountNeedsApproval(Percent(5m, noEnd: true), "USD"));
        Assert.False(settings.DiscountNeedsApproval(fixedForty, "USD"));
        Assert.True(settings.DiscountNeedsApproval(fixedForty, "EUR"));
        Assert.False(settings.OfferNeedsApproval(Percent(10m), "USD", Now.AddDays(14), Now));
        Assert.True(settings.OfferNeedsApproval(Percent(10m), "USD", Now.AddDays(15), Now));
    }

    [Fact]
    public void Settings_RefuseOutOfRangeValues()
    {
        PlatformSalesSettings settings = PlatformSalesSettings.Empty(Now);

        SubscriptionTestData.AssertCode("platform.sales_settings.value_invalid", () => settings.Update(101m, new Dictionary<string, decimal>(), false, null, null, false, Manager, Now));
        SubscriptionTestData.AssertCode("platform.sales_settings.value_invalid", () => settings.Update(null, new Dictionary<string, decimal>(), false, 0, null, false, Manager, Now));
    }

    [Fact]
    public void Promotion_GoesLiveOnlyAfterSomeoneElseApproves()
    {
        Guid basic = Guid.NewGuid();
        PlatformPromotion promotion = PlatformPromotion.Create("launch1", "Launch: first month free", Percent(100m), Now, Now.AddMonths(2), new[] { basic }, PlatformSubscriptionTerm.Monthly, true, true, 2, Sales, Now);

        Assert.Equal("LAUNCH1", promotion.Code);
        Assert.Equal("platform.promotions.not_active", promotion.Refusal(basic, PlatformSubscriptionTerm.Monthly, true, Now)!.Code);

        promotion.Submit(Sales, Now);
        SubscriptionTestData.AssertCode("platform.sales.own_submission", () => promotion.Approve(Sales, null, Now));
        promotion.Approve(Manager, "Fine", Now);

        Assert.Null(promotion.Refusal(basic, PlatformSubscriptionTerm.Monthly, true, Now));
        Assert.Equal("platform.promotions.not_for_plan", promotion.Refusal(Guid.NewGuid(), PlatformSubscriptionTerm.Monthly, true, Now)!.Code);
        Assert.Equal("platform.promotions.not_for_term", promotion.Refusal(basic, PlatformSubscriptionTerm.Annual, true, Now)!.Code);
        Assert.Equal("platform.promotions.expired", promotion.Refusal(basic, PlatformSubscriptionTerm.Monthly, true, Now.AddMonths(3))!.Code);

        promotion.Redeem(basic, PlatformSubscriptionTerm.Monthly, true, Now);
        promotion.Redeem(basic, PlatformSubscriptionTerm.Monthly, false, Now);

        Assert.Equal("platform.promotions.used_up", promotion.Refusal(basic, PlatformSubscriptionTerm.Monthly, true, Now)!.Code);
    }

    [Fact]
    public void Offer_IsIssuedUsedAndTiedToItsEmail()
    {
        PlatformOffer offer = Offer(email: "buyer@acme.test", uses: 1);

        offer.Submit(false, "hash-1", Now);

        Assert.Equal(PlatformOfferStatus.Issued, offer.Status);
        Assert.Equal("platform.offers.email_mismatch", offer.Refusal("someone@else.test", Now)!.Code);

        offer.Use("BUYER@acme.test", Now);

        Assert.Equal(PlatformOfferStatus.Used, offer.Status);
        Assert.Null(offer.TokenHash);
        Assert.Equal("platform.offers.unavailable", offer.Refusal("buyer@acme.test", Now)!.Code);
    }

    [Fact]
    public void Offer_OverTheLimit_WaitsForSomeoneElse_AndExpires()
    {
        PlatformOffer offer = Offer(email: null, uses: 2);

        offer.Submit(true, null, Now);

        Assert.Equal(PlatformOfferStatus.PendingApproval, offer.Status);
        SubscriptionTestData.AssertCode("platform.sales.own_submission", () => offer.Approve(Sales, null, "hash", Now));

        offer.Approve(Manager, "Agreed with the CEO", "hash-2", Now);

        Assert.Null(offer.Refusal(null, Now));
        Assert.Equal("platform.offers.unavailable", offer.Refusal(null, Now.AddDays(31))!.Code);
    }

    [Fact]
    public void Offer_Revoked_HasNoLinkLeft()
    {
        PlatformOffer offer = Offer(email: null, uses: 1);
        offer.Submit(false, "hash-3", Now);

        offer.Revoke(Manager, Now);

        Assert.Null(offer.TokenHash);
        Assert.Equal("platform.offers.unavailable", offer.Refusal(null, Now)!.Code);
        SubscriptionTestData.AssertCode("platform.offers.not_revocable", () => offer.Revoke(Manager, Now));
    }

    [Fact]
    public void PlanVersion_IsPublishedOnlyAfterApproval()
    {
        PlatformPlan plan = PlatformPlan.Create("PRO", "Pro", null, "USD", Sales, Now);
        plan.EditDraft(SubscriptionTestData.Terms(), Now);

        plan.SubmitDraft(Sales, Now);

        SubscriptionTestData.AssertCode("platform.plans.draft_submitted", () => plan.EditDraft(SubscriptionTestData.Terms(monthly: 12m), Now));
        SubscriptionTestData.AssertCode("platform.plans.own_submission", () => plan.ApproveDraft(Sales, Now));
        SubscriptionTestData.AssertCode("platform.plans.rejection_reason_required", () => plan.RejectDraft(Manager, " ", Now));

        plan.RejectDraft(Manager, "Price too low", Now);

        Assert.Equal(PlatformPlanVersionStatus.Draft, plan.Draft!.Status);

        plan.SubmitDraft(Sales, Now);
        plan.ApproveDraft(Manager, Now);

        Assert.Equal(PlatformPlanVersionStatus.Published, plan.Current!.Status);
        Assert.Null(plan.Draft);
    }

    private static PlatformOffer Offer(string? email, int uses)
    {
        return PlatformOffer.Prepare(
            "Acme Freight",
            email,
            "Three-year partnership",
            Guid.NewGuid(),
            PlatformSubscriptionTerm.Annual,
            20,
            Percent(35m),
            0m,
            new[] { ("Data migration", 1200m) },
            false,
            Now.AddDays(30),
            uses,
            Sales,
            Now);
    }
}
