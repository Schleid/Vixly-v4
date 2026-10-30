using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsInspectionDomainTests.cs
//
// PURPOSE:
// Proves the rules of compliance inspection: the restricted list, the rules, the selection and an inspection's life (Slice 3C).
//
// RESPONSIBILITIES:
// - Declarations are read word by word, in any of the three languages, plurals included
// - A listed item counts only where it applies; rules match value, carrier and destination
// - An inspection needs what each outcome asks for; a disposal waits for a second person who is the Owner or an administrator
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsInspectionDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Two lithium batteries and a charger", "lithium", true)]
    [InlineData("Power-bank 20000 mAh", "power bank", true)]
    [InlineData("A powerful bank statement", "power bank", false)]
    [InlineData("Médicaments pour ma mère", "médicament", true)]
    [InlineData("medicamentos y vitaminas", "medicamento", true)]
    [InlineData("Aerosols", "aerosol", true)]
    [InlineData("Shoes", "battery", false)]
    [InlineData("Cash register paper", "cash register", true)]
    public void Declarations_AreReadWordByWord(string declaration, string listed, bool named)
    {
        OperationsRestrictedItem item = OperationsRestrictedItem.Create(Tenant, "Item", listed, OperationsRestrictedItemKind.Restricted, null, null, null, Now);

        Assert.Equal(named, item.IsNamedIn(OperationsDeclarationText.Words(declaration)));
    }

    [Fact]
    public void ListedItem_AppliesOnlyToItsDestinationsAndCarriers_AndIsChecked()
    {
        OperationsRestrictedItem item = OperationsRestrictedItem.Create(Tenant, " Aerosols ", "aerosol, spray", OperationsRestrictedItemKind.Restricted, "ht, do", "DHL", "Not by air", Now);

        Assert.Equal(("Aerosols", "HT,DO", "DHL"), (item.Name, item.Countries, item.Carriers));
        Assert.True(item.AppliesTo("HT", "dhl"));
        Assert.False(item.AppliesTo("US", "DHL"));
        Assert.False(item.AppliesTo("HT", "UPS"));
        Assert.False(item.AppliesTo(null, "DHL"));
        Assert.True(OperationsRestrictedItem.Create(Tenant, "Cash", "cash", OperationsRestrictedItemKind.Prohibited, null, null, null, Now).AppliesTo(null, null));

        item.Change("Aerosols", "aerosol", OperationsRestrictedItemKind.Restricted, null, null, null, false, Now);
        Assert.False(item.AppliesTo("HT", "DHL"));

        Assert.Equal("ops.inspection.words_required", Assert.Throws<DomainException>(() => OperationsRestrictedItem.Create(Tenant, "X", " , ", OperationsRestrictedItemKind.Restricted, null, null, null, Now)).Error.Code);
        Assert.Equal("ops.inspection.country_invalid", Assert.Throws<DomainException>(() => OperationsRestrictedItem.Create(Tenant, "X", "x", OperationsRestrictedItemKind.Restricted, "XX", null, null, Now)).Error.Code);
        Assert.Equal("ops.inspection.name_required", Assert.Throws<DomainException>(() => OperationsRestrictedItem.Create(Tenant, " ", "x", OperationsRestrictedItemKind.Restricted, null, null, null, Now)).Error.Code);
        Assert.Equal("ops.inspection.text_invalid", Assert.Throws<DomainException>(() => OperationsRestrictedItem.Create(Tenant, "<b>X</b>", "x", OperationsRestrictedItemKind.Restricted, null, null, null, Now)).Error.Code);
    }

    [Fact]
    public void Rules_MatchValueCarrierAndDestination_AndCheckTheirValue()
    {
        OperationsInspectionSubject subject = new("Shoes", 750m, "FedEx Ground", "HT");

        Assert.True(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ValueAtLeast, "500", Now).Matches(subject));
        Assert.False(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ValueAtLeast, "1000.50", Now).Matches(subject));
        Assert.True(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.Carrier, "fedex ground", Now).Matches(subject));
        Assert.True(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.DestinationCountry, "ht", Now).Matches(subject));
        Assert.False(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.DestinationCountry, "DO", Now).Matches(subject));
        Assert.False(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.DestinationCountry, "HT", Now).Matches(subject with { CountryCode = null }));

        Assert.Equal("ops.inspection.rule_value_invalid", Assert.Throws<DomainException>(() => OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ValueAtLeast, "1,000", Now)).Error.Code);
        Assert.Equal("ops.inspection.rule_value_invalid", Assert.Throws<DomainException>(() => OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ValueAtLeast, "0", Now)).Error.Code);
        Assert.Equal("ops.inspection.country_invalid", Assert.Throws<DomainException>(() => OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.DestinationCountry, "Haiti", Now)).Error.Code);
        Assert.Null(OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ListedItems, "ignored", Now).Value);
    }

    [Fact]
    public void Selector_NamesTheFirstMatch_AndSkipsWhatIsNotInUse()
    {
        OperationsRestrictedItem batteries = OperationsRestrictedItem.Create(Tenant, "Lithium batteries", "lithium, battery, batteries", OperationsRestrictedItemKind.Restricted, null, null, null, Now);
        OperationsRestrictedItem food = OperationsRestrictedItem.Create(Tenant, "Food", "cheese", OperationsRestrictedItemKind.Restricted, "DO", null, null, Now);
        OperationsInspectionRule listed = OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ListedItems, null, Now);
        OperationsInspectionRule value = OperationsInspectionRule.Create(Tenant, OperationsInspectionRuleKind.ValueAtLeast, "100", Now);
        OperationsInspectionRule[] rules = { value, listed };
        OperationsRestrictedItem[] items = { batteries, food };

        Assert.Equal("listed:Lithium batteries", OperationsInspectionSelector.FirstMatch(new("Laptop battery", 50m, "UPS", "HT"), rules, items));
        Assert.Equal("value:100", OperationsInspectionSelector.FirstMatch(new("Cheese", 150m, "UPS", "HT"), rules, items));
        Assert.Null(OperationsInspectionSelector.FirstMatch(new("Cheese", 50m, "UPS", "HT"), rules, items));
        Assert.Equal("listed:Food", OperationsInspectionSelector.FirstMatch(new("Cheese", 50m, "UPS", "DO"), rules, items));

        listed.ChangeActive(false, Now);
        Assert.Null(OperationsInspectionSelector.FirstMatch(new("Laptop battery", 50m, "UPS", "HT"), rules, items));
        Assert.Equal(new[] { batteries }, OperationsInspectionSelector.Applicable(new("Shoes", 1m, "UPS", "HT"), items));
    }

    [Fact]
    public void Inspection_RecordsEachOutcome_WithWhatItNeeds()
    {
        Guid inspector = Guid.NewGuid();
        Assert.Equal("ops.inspection.flag_reason_required",
            Assert.Throws<DomainException>(() => OperationsInspection.Select(Tenant, Parcel(), "flagged", " ", inspector, Now)).Error.Code);

        OperationsInspection inspection = OperationsInspection.Select(Tenant, Parcel(), "rule", "listed:Lithium batteries", null, Now);
        Assert.Equal(OperationsInspectionStatus.Waiting, inspection.Status);
        Assert.Equal("ops.inspection.not_in_progress", Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("passed"), "USD", Now)).Error.Code);

        inspection.Start(inspector, Now);
        inspection.Start(inspector, Now);
        Assert.Equal("ops.inspection.not_waiting", Assert.Throws<DomainException>(() => inspection.Start(Guid.NewGuid(), Now)).Error.Code);

        Assert.Equal("ops.inspection.notes_required", Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("passed_with_notes"), "USD", Now)).Error.Code);
        Assert.Equal("ops.inspection.hold_reason_invalid", Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("held") with { CustomerAction = "Send the invoice" }, "USD", Now)).Error.Code);
        Assert.Equal("ops.inspection.customer_action_required",
            Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("held") with { HoldReason = OperationsHoldReasons.InspectionDocuments }, "USD", Now)).Error.Code);
        Assert.Equal("ops.inspection.fee_invalid", Assert.Throws<DomainException>(() =>
            inspection.Finish(inspector, Outcome("held") with { HoldReason = OperationsHoldReasons.InspectionFee, CustomerAction = "Pay the fee" }, "USD", Now)).Error.Code);
        Assert.Equal("ops.inspection.result_invalid", Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("lost"), "USD", Now)).Error.Code);

        inspection.Finish(inspector, Outcome("held") with { HoldReason = OperationsHoldReasons.InspectionFee, CustomerAction = "Pay the handling fee", Fee = 12.345m, ItemsFound = new[] { "Lithium batteries", "Lithium batteries" } }, "USD", Now);
        Assert.Equal((OperationsInspectionStatus.Done, "held", OperationsHoldReasons.InspectionFee), (inspection.Status, inspection.Result, inspection.HoldReason));
        Assert.Equal((12.35m, "USD", "Lithium batteries"), (inspection.Fee!.Value, inspection.FeeCurrency, inspection.ItemsFound));
        Assert.False(inspection.IsOpen);
    }

    [Fact]
    public void Disposal_NeedsTheCall_AndTheApprovalOfAnotherPersonWhoIsOwnerOrAdministrator()
    {
        Guid inspector = Guid.NewGuid();
        Guid owner = Guid.NewGuid();
        OperationsInspection inspection = OperationsInspection.Select(Tenant, Parcel(), "flagged", "Smells of fuel", inspector, Now);
        inspection.Start(inspector, Now);

        Assert.Equal("ops.inspection.call_required", Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("refused_disposed"), "USD", Now)).Error.Code);
        Assert.Equal("ops.inspection.call_required",
            Assert.Throws<DomainException>(() => inspection.Finish(inspector, Outcome("refused_disposed") with { CalledCustomerUtc = Now.AddDays(2), CallNotes = "Agreed" }, "USD", Now)).Error.Code);

        inspection.Finish(inspector, Outcome("refused_disposed") with { CalledCustomerUtc = Now.Date, CallNotes = "Customer agreed by phone" }, "USD", Now);
        Assert.Equal(OperationsInspectionStatus.AwaitingApproval, inspection.Status);

        Assert.Equal("ops.inspection.same_approver", Assert.Throws<DomainException>(() => inspection.ApproveDisposal(inspector, true, Now)).Error.Code);
        Assert.Equal("ops.inspection.approver_not_allowed", Assert.Throws<DomainException>(() => inspection.ApproveDisposal(Guid.NewGuid(), false, Now)).Error.Code);

        inspection.DeclineDisposal(owner, true, Now);
        Assert.Equal((OperationsInspectionStatus.InProgress, (string?)null), (inspection.Status, inspection.Result));

        inspection.Finish(owner, Outcome("refused_disposed") with { CalledCustomerUtc = Now.AddHours(20), CallNotes = "Agreed again" }, "USD", Now);
        Assert.Equal("ops.inspection.same_approver", Assert.Throws<DomainException>(() => inspection.ApproveDisposal(inspector, true, Now)).Error.Code);
        inspection.DeclineDisposal(Guid.NewGuid(), true, Now);
        inspection.Finish(inspector, Outcome("refused_disposed") with { CalledCustomerUtc = Now, CallNotes = "Agreed again" }, "USD", Now);
        inspection.ApproveDisposal(owner, true, Now);
        Assert.Equal((OperationsInspectionStatus.Done, owner), (inspection.Status, inspection.ApprovedByUserId!.Value));
        Assert.Equal("ops.inspection.not_awaiting_approval", Assert.Throws<DomainException>(() => inspection.ApproveDisposal(owner, true, Now)).Error.Code);
    }

    [Fact]
    public void RandomShare_IsFromZeroToAHundred()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal(5, settings.InspectionPercent);
        settings.ChangeInspectionPercent(0, Now);
        Assert.Equal(0, settings.InspectionPercent);
        Assert.Equal("ops.inspection.share_invalid", Assert.Throws<DomainException>(() => settings.ChangeInspectionPercent(101, Now)).Error.Code);
    }

    private static OperationsInspectionOutcome Outcome(string result) =>
        new(result, Array.Empty<string>(), null, null, OperationsHoldReasons.None, null, null, null);

    private static OperationsParcel Parcel() =>
        OperationsParcel.Receive(Tenant, "PK26000001", OperationsInboundDelivery.Open(Tenant, Guid.NewGuid(), "UPS", Guid.NewGuid(), "key", Now), "UPS", "1Z999AA10123456784", false, Now);
}
