using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsHoldDomainTests.cs
//
// PURPOSE:
// Proves the rules of customers' answers to holds and of reminders (Slice 3E).
//
// RESPONSIBILITIES:
// - Each hold takes its own kind of answer; others are not answered online
// - An answer is reviewed once: accepted, or declined with a note
// - Reminders go one at a time; the unclaimed days start at 30
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsHoldDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EachHold_TakesItsOwnKindOfAnswer()
    {
        OperationsHoldDocument pdf = new("answers/1", "application/pdf", "C:\\Users\\ana\\permit.pdf", 1200, new string('a', 64));

        OperationsHoldAnswer document = OperationsHoldAnswer.Create(Held(OperationsHoldReasons.InspectionPermit), OperationsHoldReasons.InspectionPermit, null, null, "USD", null, " Here it is ", pdf, Now);
        Assert.Equal((OperationsHoldAnswerKind.Document, "permit.pdf", "Here it is"), (document.Kind, document.DocumentName, document.Note));
        Assert.Equal("ops.holds.document_required",
            Assert.Throws<DomainException>(() => OperationsHoldAnswer.Create(Held(OperationsHoldReasons.InspectionDocuments), OperationsHoldReasons.InspectionDocuments, null, null, "USD", null, null, null, Now)).Error.Code);

        OperationsHoldAnswer value = OperationsHoldAnswer.Create(Held(OperationsHoldReasons.InspectionValue), OperationsHoldReasons.InspectionValue, "Two phones", 450.555m, "USD", null, null, null, Now);
        Assert.Equal(("Two phones", 450.56m, "USD"), (value.Contents, value.DeclaredValue!.Value, value.CurrencyCode));
        Assert.Equal("ops.holds.contents_required",
            Assert.Throws<DomainException>(() => OperationsHoldAnswer.Create(Held(OperationsHoldReasons.InspectionValue), OperationsHoldReasons.InspectionValue, " ", 10m, "USD", null, null, null, Now)).Error.Code);
        Assert.Equal("ops.holds.value_invalid",
            Assert.Throws<DomainException>(() => OperationsHoldAnswer.Create(Held(OperationsHoldReasons.InspectionValue), OperationsHoldReasons.InspectionValue, "x", -1m, "USD", null, null, null, Now)).Error.Code);

        Assert.Equal("dispose", OperationsHoldAnswer.Create(Held(OperationsHoldReasons.Damaged), OperationsHoldReasons.Damaged, null, null, "USD", "dispose", null, null, Now).Choice);
        Assert.Equal("ops.holds.choice_invalid",
            Assert.Throws<DomainException>(() => OperationsHoldAnswer.Create(Held(OperationsHoldReasons.Damaged), OperationsHoldReasons.Damaged, null, null, "USD", "keep", null, null, Now)).Error.Code);

        // Not on hold for that reason, a hold answered elsewhere, or a package without a customer.
        Assert.Equal("ops.holds.not_answerable",
            Assert.Throws<DomainException>(() => OperationsHoldAnswer.Create(Held(OperationsHoldReasons.Damaged), OperationsHoldReasons.InspectionPermit, null, null, "USD", null, null, pdf, Now)).Error.Code);
        Assert.Equal("ops.holds.not_answerable",
            Assert.Throws<DomainException>(() => OperationsHoldAnswer.Create(Held(OperationsHoldReasons.InspectionFee), OperationsHoldReasons.InspectionFee, null, null, "USD", null, null, null, Now)).Error.Code);
        Assert.Null(OperationsHoldAnswer.KindFor(OperationsHoldReasons.AwaitingCustomerInformation));
    }

    [Fact]
    public void AnAnswer_IsReviewedOnce()
    {
        OperationsHoldAnswer answer = OperationsHoldAnswer.Create(Held(OperationsHoldReasons.Damaged), OperationsHoldReasons.Damaged, null, null, "USD", "ship_as_is", null, null, Now);
        Assert.Equal("ops.holds.review_note_required", Assert.Throws<DomainException>(() => answer.Decline(Guid.NewGuid(), " ", Now)).Error.Code);

        answer.Decline(Guid.NewGuid(), "Please choose again after our call", Now);
        Assert.Equal(OperationsHoldAnswerStatus.Declined, answer.Status);
        Assert.Equal("ops.holds.answer_not_open", Assert.Throws<DomainException>(() => answer.Accept(Guid.NewGuid(), null, Now)).Error.Code);

        OperationsHoldAnswer second = OperationsHoldAnswer.Create(Held(OperationsHoldReasons.Damaged), OperationsHoldReasons.Damaged, null, null, "USD", "return", null, null, Now);
        second.Replace(Now);
        Assert.Equal(OperationsHoldAnswerStatus.Replaced, second.Status);
    }

    [Fact]
    public void Reminders_GoOneAtATime_AndUnclaimedDaysStartAt30()
    {
        OperationsParcel parcel = Held(OperationsHoldReasons.AwaitingCustomerInformation);
        Assert.False(parcel.Remind(2, Now));
        Assert.True(parcel.Remind(1, Now));
        Assert.False(parcel.Remind(1, Now));
        Assert.True(parcel.Remind(2, Now) && parcel.Remind(3, Now) && parcel.Remind(4, Now));
        Assert.False(parcel.Remind(5, Now));
        Assert.Equal(4, parcel.RemindersSent);

        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal(30, settings.UnclaimedDays);
        settings.ChangeUnclaimedDays(45, Now);
        Assert.Equal(45, settings.UnclaimedDays);
        Assert.Equal("ops.holds.days_invalid", Assert.Throws<DomainException>(() => settings.ChangeUnclaimedDays(20, Now)).Error.Code);
    }

    // A package of a customer, at its warehouse, on hold for the reason.
    private static OperationsParcel Held(OperationsHoldReasons reason)
    {
        OperationsParcel parcel = OperationsParcel.Receive(Tenant, "PK26000001", OperationsInboundDelivery.Open(Tenant, Guid.NewGuid(), "UPS", Guid.NewGuid(), "key", Now), "UPS", "1Z999AA10123456784", false, Now);
        parcel.JoinShipment(Vixly.Operations.Domain.Shipments.OperationsShipment.Open(Tenant, Guid.NewGuid(), parcel.WarehouseId!.Value, Now), Now);
        parcel.Project(new OperationsParcelState(OperationsMilestone.ReceivedAtOrigin, OperationsParcelFlags.OnHold, reason, Now), Now);
        return parcel;
    }
}
