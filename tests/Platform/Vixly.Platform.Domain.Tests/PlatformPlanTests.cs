using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformPlanTests.cs
//
// PURPOSE:
// Proves plans, translations and versions (D-S1, D-S2, D-S7).
//
// RESPONSIBILITIES:
// - A plan starts with a draft; publishing makes it current and supersedes the previous version
// - The currency is fixed once a version is published
// - Translations fall back to English; English is required
// - Retired plans cannot be chosen
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformPlanTests
{
    private static readonly DateTimeOffset Now = SubscriptionTestData.Now;

    [Fact]
    public void Create_StartsWithAnEnglishName_AndADraftVersionOne()
    {
        PlatformPlan plan = PlatformPlan.Create(" growth ", "Growth", null, "cad", SubscriptionTestData.Actor, Now);

        Assert.Equal("GROWTH", plan.Code);
        Assert.Equal("Growth", plan.NameIn(LanguageCode.Fr));
        Assert.Null(plan.Current);
        Assert.Equal(1, plan.Draft!.Number);
        Assert.Equal("CAD", plan.Draft.CurrencyCode);
        Assert.False(plan.CanBeChosen);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("-BAD")]
    [InlineData("HAS SPACE")]
    public void Create_RefusesBadCodes(string code)
    {
        SubscriptionTestData.AssertCode("platform.plans.code_invalid", () => PlatformPlan.Create(code, "Name", null, "USD", SubscriptionTestData.Actor, Now));
    }

    [Fact]
    public void Create_RefusesUnknownCurrency()
    {
        SubscriptionTestData.AssertCode("platform.plans.currency_invalid", () => PlatformPlan.Create("BASIC", "Name", null, "XYZ", SubscriptionTestData.Actor, Now));
    }

    [Fact]
    public void Publish_NeedsAPrice()
    {
        PlatformPlan plan = PlatformPlan.Create("BASIC", "Basic", null, "USD", SubscriptionTestData.Actor, Now);

        SubscriptionTestData.AssertCode("platform.plans.price_required", () => plan.PublishDraft(SubscriptionTestData.Actor, Now));
    }

    [Fact]
    public void Publish_MakesTheDraftCurrent_AndSupersedesThePreviousVersion()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        PlatformPlanVersion first = plan.Current!;

        plan.StartDraft(Now);
        plan.EditDraft(SubscriptionTestData.Terms(monthly: 12m), Now);
        PlatformPlanVersion second = plan.PublishDraft(SubscriptionTestData.Actor, Now);

        Assert.Equal(2, second.Number);
        Assert.Same(second, plan.Current);
        Assert.Equal(PlatformPlanVersionStatus.Superseded, first.Status);
        Assert.Equal(10m, first.MonthlyPricePerLicence);
        Assert.Null(plan.Draft);
    }

    [Fact]
    public void StartDraft_CopiesTheCurrentTerms_AndAllowsOnlyOneDraft()
    {
        PlatformPlan plan = SubscriptionTestData.Published(terms: SubscriptionTestData.Terms(features: PlatformPlanFeature.DataExport));

        PlatformPlanVersion draft = plan.StartDraft(Now);

        Assert.Equal(plan.Current!.MonthlyPricePerLicence, draft.MonthlyPricePerLicence);
        Assert.Equal(plan.Current.Terms.Features, draft.Terms.Features);
        Assert.True(draft.Includes(PlatformPlanFeature.DataExport));
        SubscriptionTestData.AssertCode("platform.plans.draft_exists", () => plan.StartDraft(Now));
    }

    [Fact]
    public void Currency_IsFixed_OnceAVersionIsPublished()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        plan.StartDraft(Now);

        SubscriptionTestData.AssertCode("platform.plans.currency_fixed", () => plan.EditDraft(SubscriptionTestData.Terms(currency: "EUR"), Now));
    }

    [Fact]
    public void Terms_RefuseAMinimumAboveTheDeviceLimit_AndNegativePrices()
    {
        SubscriptionTestData.AssertCode("platform.plans.minimum_above_device_limit", () => SubscriptionTestData.Terms(minimum: 5, maxDevices: 4));
        SubscriptionTestData.AssertCode("platform.plans.price_invalid", () => SubscriptionTestData.Terms(monthly: -1m));
        SubscriptionTestData.AssertCode("platform.plans.trial_days_invalid", () => SubscriptionTestData.Terms(trialDays: 400));
    }

    [Fact]
    public void Translations_FallBackToEnglish_AndEnglishCannotBeEmptied()
    {
        PlatformPlan plan = SubscriptionTestData.Published();

        plan.SetTranslation(LanguageCode.Fr, "De base", null, Now);

        Assert.Equal("De base", plan.NameIn(LanguageCode.Fr));
        Assert.Equal("Description", plan.DescriptionIn(LanguageCode.Fr));
        Assert.Equal("BASIC plan", plan.NameIn(LanguageCode.Es));
        SubscriptionTestData.AssertCode("platform.plans.english_name_required", () => plan.SetTranslation(LanguageCode.En, " ", null, Now));

        plan.SetTranslation(LanguageCode.Fr, null, null, Now);
        Assert.Equal("BASIC plan", plan.NameIn(LanguageCode.Fr));
    }

    [Fact]
    public void Retire_StopsNewChoices_AndCanBeUndone()
    {
        PlatformPlan plan = SubscriptionTestData.Published();

        plan.Retire(Now);

        Assert.False(plan.CanBeChosen);
        SubscriptionTestData.AssertCode("platform.plans.already_retired", () => plan.Retire(Now));

        plan.Reactivate(Now);
        Assert.True(plan.CanBeChosen);
    }

    [Fact]
    public void DiscardDraft_KeepsThePublishedVersion()
    {
        PlatformPlan plan = SubscriptionTestData.Published();
        plan.StartDraft(Now);

        plan.DiscardDraft(Now);

        Assert.Null(plan.Draft);
        Assert.Single(plan.Versions);
    }
}
