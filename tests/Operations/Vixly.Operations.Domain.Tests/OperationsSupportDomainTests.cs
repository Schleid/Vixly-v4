using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsSupportDomainTests.cs
//
// PURPOSE:
// Proves the outside accounts and call tickets of the support agent design (owner, 2026-10-29, section 9).
//
// RESPONSIBILITIES:
// - Hours: days, times, the end date; refused shapes
// - Outside accounts: never the Owner or an Administrator; never made an Administrator
// - Call tickets are desk tickets: a topic, a note keeps them open, taken, moved, escalated once per step
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsSupportDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public void Hours_CoverTheirDays_TheirTimes_UntilTheEndDate()
    {
        // Monday to Friday, 08:00 to 17:00, until 30 November 2026.
        OperationsOutsideHours hours = OperationsOutsideHours.Create(new DateOnly(2026, 11, 30), 31, 8 * 60, 17 * 60);

        Assert.True(hours.Covers(new DateTime(2026, 10, 29, 8, 0, 0)));
        Assert.False(hours.Covers(new DateTime(2026, 10, 29, 17, 0, 0)));
        Assert.False(hours.Covers(new DateTime(2026, 10, 29, 7, 59, 0)));
        Assert.False(hours.Covers(new DateTime(2026, 10, 31, 10, 0, 0)));
        Assert.True(hours.Covers(new DateTime(2026, 11, 30, 10, 0, 0)));
        Assert.False(hours.Covers(new DateTime(2026, 12, 1, 10, 0, 0)));
        Assert.True(OperationsOutsideHours.Create(null, OperationsOutsideHours.EveryDay, null, null).Covers(new DateTime(2026, 11, 1, 23, 59, 0)));
        Assert.True(OperationsOutsideHours.Create(null, 64, 0, OperationsOutsideHours.MinutesInADay).Covers(new DateTime(2026, 11, 1, 23, 59, 0)));
    }

    [Theory]
    [InlineData(0, null, null, "ops.staff.outside.days_required")]
    [InlineData(128, null, null, "ops.staff.outside.days_required")]
    [InlineData(31, 480, null, "ops.staff.outside.hours_invalid")]
    [InlineData(31, 600, 480, "ops.staff.outside.hours_invalid")]
    [InlineData(31, 0, 1441, "ops.staff.outside.hours_invalid")]
    public void Hours_OutsideTheRules_AreRefused(int days, int? from, int? to, string code)
    {
        Assert.Equal(code, Assert.Throws<DomainException>(() => OperationsOutsideHours.Create(null, days, from, to)).Error.Code);
    }

    [Fact]
    public void OutsideAccount_WorksOnlyInItsHours_AndIsNeverAnAdministrator()
    {
        OperationsUser agent = OperationsUser.InviteStaff(Tenant, "Call Agent", EmailAddress.Create("agent@callcentre.example"), LanguageCode.En, Now);
        Assert.True(agent.MayWorkAt(new DateTime(2026, 11, 1, 3, 0, 0)));

        agent.MakeOutside(OperationsOutsideHours.Create(null, 31, 8 * 60, 17 * 60), new DateOnly(2026, 10, 29), Now);

        Assert.True(agent.IsOutside);
        Assert.False(agent.MayWorkAt(new DateTime(2026, 11, 1, 10, 0, 0)));
        Assert.True(agent.MayWorkAt(new DateTime(2026, 11, 2, 10, 0, 0)));
        Assert.Equal("ops.staff.outside.not_for_this_account", Assert.Throws<DomainException>(() => agent.SetAdministrator(true, Now)).Error.Code);
        Assert.Equal("ops.staff.outside.ends_in_the_past", Assert.Throws<DomainException>(() =>
            agent.MakeOutside(OperationsOutsideHours.Create(new DateOnly(2026, 10, 28), 31, null, null), new DateOnly(2026, 10, 29), Now)).Error.Code);

        agent.MakeInside(Now);
        Assert.False(agent.IsOutside);
        Assert.Null(agent.OutsideHours);

        OperationsUser owner = OperationsUser.InviteOwner(Tenant, "Grace Hopper", EmailAddress.Create("grace@example.com"), LanguageCode.En, Now);
        Assert.Equal("ops.staff.outside.not_for_this_account", Assert.Throws<DomainException>(() =>
            owner.MakeOutside(OperationsOutsideHours.Create(null, 31, null, null), new DateOnly(2026, 10, 29), Now)).Error.Code);
    }

    [Fact]
    public void CallTicket_IsADeskTicket_ForItsDepartment_TakenAndEscalatedOnce()
    {
        Guid customer = Guid.NewGuid();
        Guid agent = Guid.NewGuid();
        Guid warehouse = Guid.NewGuid();

        Assert.Equal("ops.tickets.details_invalid", Assert.Throws<DomainException>(() =>
            OperationsTicket.FromCall(Tenant, customer, warehouse, null, null, "VX1234567890X", OperationsTicketTopic.Billing, agent, "Change pickup", Now)).Error.Code);
        Assert.Equal("ops.tickets.message_invalid", Assert.Throws<DomainException>(() =>
            OperationsTicket.FromCall(Tenant, customer, warehouse, Guid.NewGuid(), null, "VX1234567890X", OperationsTicketTopic.Billing, agent, "  ", Now)).Error.Code);
        Assert.Equal("ops.tickets.topic_invalid", Assert.Throws<DomainException>(() =>
            OperationsTicket.FromCall(Tenant, customer, warehouse, Guid.NewGuid(), null, "VX1234567890X", (OperationsTicketTopic)99, agent, "Hi", Now)).Error.Code);

        OperationsTicket ticket = OperationsTicket.FromCall(Tenant, customer, warehouse, Guid.NewGuid(), null, " VX1234567890X ", OperationsTicketTopic.Billing, agent, " Disputes the amount ", Now);
        Assert.Equal(("VX1234567890X", OperationsTicketTopic.Billing, OperationsTicketSource.Agent, false), (ticket.TrackingNumber, ticket.Topic, ticket.OpenedBy, ticket.FromCustomer));
        Assert.Equal("Disputes the amount", Assert.Single(ticket.Messages).Text);

        // Nobody took it in 4 hours: step 1; still not resolved after 3 days: step 2; each once.
        Assert.Equal(0, ticket.EscalationDue(4, 3, Now.AddHours(3)));
        Assert.Equal(1, ticket.EscalationDue(4, 3, Now.AddHours(5)));
        ticket.Escalated(1, Now.AddHours(5));
        Assert.Equal(0, ticket.EscalationDue(4, 3, Now.AddHours(6)));
        Assert.Equal(2, ticket.EscalationDue(4, 3, Now.AddDays(3)));
        ticket.Escalated(2, Now.AddDays(3));
        Assert.Equal(0, ticket.EscalationDue(4, 3, Now.AddDays(9)));

        // A staff note keeps an agent's ticket open (no customer to wait for) and takes it; moving it frees it for the new department.
        Guid staff = Guid.NewGuid();
        ticket.StaffReply(staff, "Called the customer back", Now.AddDays(4));
        Assert.Equal((OperationsTicketStatus.Open, staff), (ticket.Status, ticket.TakenByUserId));
        ticket.Move(OperationsTicketTopic.Other, Now.AddDays(4));
        Assert.Equal((OperationsTicketTopic.Other, (Guid?)null), (ticket.Topic, ticket.TakenByUserId));
        ticket.Take(staff, Now.AddDays(4));
        ticket.Resolve(staff, null, "Done", Now.AddDays(5));
        Assert.Equal(OperationsTicketStatus.Resolved, ticket.Status);
    }

    [Fact]
    public void TicketLimits_AreKeptWithinTheirRange()
    {
        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(Tenant, Now);
        Assert.Equal((4, 3), (settings.TicketTakeHours, settings.TicketResolveDays));

        settings.ChangeTickets(8, 5, Now);
        Assert.Equal((8, 5), (settings.TicketTakeHours, settings.TicketResolveDays));
        Assert.Equal("ops.tickets.limits_invalid", Assert.Throws<DomainException>(() => settings.ChangeTickets(0, 5, Now)).Error.Code);
        Assert.Equal("ops.tickets.limits_invalid", Assert.Throws<DomainException>(() => settings.ChangeTickets(8, 31, Now)).Error.Code);
    }
}
