using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsEventDomainTests.cs
//
// PURPOSE:
// Proves the rules of operational events (Slice 6C, E-1 to E-3).
//
// RESPONSIBILITIES:
// - A company's own cause: its kind, its words, switched off
// - The standard causes a company switches off
// - The manual codes: a note internal, a delay and an exception shown to the customer with optional words
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsEventDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ACompanyCause_HasItsKindAndWords_FallsBackToEnglish_AndIsSwitchedOff()
    {
        Assert.Equal(OperationsEventErrors.KindInvalid, Assert.Throws<DomainException>(() => OperationsEventReason.Create(Tenant, OperationsEventKind.Note, "x", null, null, Now)).Error);
        Assert.Equal(OperationsEventErrors.ReasonRequired, Assert.Throws<DomainException>(() => OperationsEventReason.Create(Tenant, OperationsEventKind.Delay, " ", "x", null, Now)).Error);
        Assert.Equal(OperationsEventErrors.TextInvalid, Assert.Throws<DomainException>(() => OperationsEventReason.Create(Tenant, OperationsEventKind.Delay, "<i>x</i>", null, null, Now)).Error);

        OperationsEventReason reason = OperationsEventReason.Create(Tenant, OperationsEventKind.Exception, " Wrong size declared ", "Mauvaise taille déclarée", null, Now);
        Assert.Matches("^c_[0-9a-f]{12}$", reason.Code);
        Assert.Equal(("Wrong size declared", "Mauvaise taille déclarée", "Wrong size declared", true), (reason.In("en"), reason.In("fr"), reason.In("es"), reason.IsActive));
        Assert.False(OperationsEventReasons.IsStandard(reason.Code));
        reason.Change("Wrong size", null, "Tamaño equivocado", false, Now);
        Assert.Equal(("Tamaño equivocado", false), (reason.In("es"), reason.IsActive));
    }

    [Fact]
    public void TheCompany_SwitchesOffStandardCauses_AndTheEmails()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal((true, 0), (settings.EventEmails, settings.DisabledEventCauseList().Count));
        Assert.Equal(OperationsEventErrors.ReasonRequired, Assert.Throws<DomainException>(() => settings.ChangeEvents(true, new[] { "c_123456789012" }, Now)).Error);
        settings.ChangeEvents(false, new[] { "weather", " holiday", "weather" }, Now);
        Assert.Equal((false, "holiday,weather"), (settings.EventEmails, settings.DisabledEventCauses));
        settings.ChangeEvents(true, null, Now);
        Assert.Null(settings.DisabledEventCauses);
        Assert.Empty(OperationsEventReasons.Delay.Intersect(OperationsEventReasons.Exception));
    }

    [Fact]
    public void ManualCodes_AreANoteInternal_AndADelayOrExceptionForTheCustomer_WithOptionalWords()
    {
        Assert.Equal(OperationsTrackingVisibility.Internal, OperationsTrackingCodes.Find(OperationsTrackingCodes.Note)!.Visibility);
        Assert.All(new[] { OperationsTrackingCodes.Delayed, OperationsTrackingCodes.ExceptionRaised }, code =>
        {
            OperationsTrackingCode found = OperationsTrackingCodes.Find(code)!;
            Assert.Equal((OperationsTrackingVisibility.Customer, (OperationsMilestone?)null), (found.Visibility, found.Milestone));
            Assert.DoesNotContain("source", found.CustomerArguments);
        });

        OperationsTrackingDraft Draft(Dictionary<string, string> arguments) => new(OperationsTrackingSubject.Shipment, Guid.NewGuid(), OperationsTrackingCodes.Delayed, arguments, Now,
            "evt:" + Guid.NewGuid().ToString("N"), OperationsTrackingActor.Staff, OperationsTrackingSource.Manual) { ActorUserId = Guid.NewGuid() };
        Assert.Equal(OperationsTrackingErrors.ArgumentMissing, Assert.Throws<DomainException>(() => OperationsTrackingEvent.Record(Tenant, Draft(new() { ["message"] = "x" }), Now)).Error);
        Assert.Equal(OperationsTrackingErrors.ArgumentUnknown, Assert.Throws<DomainException>(() => OperationsTrackingEvent.Record(Tenant, Draft(new() { ["cause"] = "weather", ["x"] = "y" }), Now)).Error);
        OperationsTrackingEvent recorded = OperationsTrackingEvent.Record(Tenant, Draft(new() { ["cause"] = "weather" }) with { CausedByEventId = Guid.Empty }, Now);
        Assert.Equal(("weather", Guid.Empty), (recorded.Arguments()["cause"], recorded.CausedByEventId!.Value));
        Assert.Equal("Storm", OperationsTrackingEvent.Record(Tenant, Draft(new() { ["cause"] = "weather", ["message"] = "Storm" }), Now).Arguments()["message"]);
    }
}
