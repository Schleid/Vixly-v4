using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsTicketDomainTests.cs
//
// PURPOSE:
// Proves the rules of a customer's ticket about a missing package (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - Opened with what the customer knows, within limits
// - Staff answer, resolve; the customer writing back opens it again
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsTicketDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private static OperationsTicketDetails Details(DateOnly? delivered = null, string? contents = "Tape, 12 rolls") =>
        new("Amazon", " tba 3012 9988 77 ", "Amazon", "113-2223334", delivered ?? new DateOnly(2026, 10, 17), contents);

    [Fact]
    public void Open_KeepsWhatTheCustomerKnows_TheTrackingNumberNormalised()
    {
        OperationsTicket ticket = OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(), "  The carrier says front desk. ", Now);

        Assert.Equal(OperationsTicketStatus.Open, ticket.Status);
        Assert.Equal("TBA3012998877", ticket.TrackingNumber);
        Assert.Equal(("Amazon", "113-2223334", new DateOnly(2026, 10, 17)), (ticket.Store, ticket.OrderNumber, ticket.DeliveredOn));
        OperationsTicketMessage first = Assert.Single(ticket.Messages);
        Assert.Equal((false, "The carrier says front desk."), (first.FromStaff, first.Text));
    }

    [Fact]
    public void Open_RefusesMissingDetails_AndADeliveryDayOutOfRange()
    {
        Assert.Equal("ops.tickets.details_invalid", Assert.Throws<DomainException>(() => OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(contents: " "), null, Now)).Error.Code);
        Assert.Equal("ops.tickets.details_invalid", Assert.Throws<DomainException>(() => OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(new DateOnly(2026, 10, 25)), null, Now)).Error.Code);
        Assert.Equal("ops.tickets.details_invalid", Assert.Throws<DomainException>(() => OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(new DateOnly(2025, 9, 1)), null, Now)).Error.Code);
        Assert.Equal("ops.tickets.details_invalid", Assert.Throws<DomainException>(() => OperationsTicket.Open(Tenant, Guid.Empty, Guid.NewGuid(), Details(), null, Now)).Error.Code);
        Assert.Empty(OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(), null, Now).Messages);
    }

    [Fact]
    public void Answers_MoveTheStatus_TheCustomerReopensAResolvedTicket()
    {
        Guid staff = Guid.NewGuid();
        Guid parcel = Guid.NewGuid();
        OperationsTicket ticket = OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(), null, Now);

        ticket.StaffReply(staff, "Which floor was it left on?", Now);
        Assert.Equal(OperationsTicketStatus.Answered, ticket.Status);
        ticket.CustomerReply("Lobby.", Now);
        Assert.Equal(OperationsTicketStatus.Open, ticket.Status);
        ticket.Resolve(staff, parcel, "Found it on the undeclared shelf.", Now);
        Assert.Equal((OperationsTicketStatus.Resolved, (Guid?)parcel, (DateTimeOffset?)Now), (ticket.Status, ticket.ParcelId, ticket.ResolvedUtc));
        ticket.StaffReply(staff, "It ships on Friday.", Now);
        Assert.Equal(OperationsTicketStatus.Resolved, ticket.Status);
        ticket.CustomerReply("Thanks, one more is missing.", Now);
        Assert.Equal((OperationsTicketStatus.Open, (DateTimeOffset?)null, (Guid?)parcel), (ticket.Status, ticket.ResolvedUtc, ticket.ParcelId));
        Assert.Equal(5, ticket.Messages.Count);
        Assert.True(ticket.Messages[0].FromStaff);
        Assert.Equal(staff, ticket.Messages[0].UserId);

        // The package linked stays: never replaced by another; the same form's key is a repeat.
        Assert.Equal("ops.tickets.already_linked", Assert.Throws<DomainException>(() => ticket.Resolve(staff, Guid.NewGuid(), null, Now)).Error.Code);
        ticket.Resolve(staff, parcel, null, Now);
        ticket.Keyed("form-key-1");
        Assert.True(ticket.Repeats("form-key-1"));
        Assert.False(ticket.Repeats("form-key-2"));
    }

    [Fact]
    public void Messages_AreWithinLimits()
    {
        OperationsTicket ticket = OperationsTicket.Open(Tenant, Guid.NewGuid(), Guid.NewGuid(), Details(), null, Now);

        Assert.Equal("ops.tickets.message_invalid", Assert.Throws<DomainException>(() => ticket.CustomerReply(" ", Now)).Error.Code);
        Assert.Equal("ops.tickets.message_invalid", Assert.Throws<DomainException>(() => ticket.CustomerReply(new string('a', OperationsTicket.TextMaxLength + 1), Now)).Error.Code);

        for (int index = 0; index < OperationsTicket.MaxMessages; index++)
        {
            ticket.CustomerReply("Still missing.", Now);
        }

        Assert.Equal("ops.tickets.too_many_messages", Assert.Throws<DomainException>(() => ticket.CustomerReply("Again.", Now)).Error.Code);
    }
}
