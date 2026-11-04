namespace Vixly.Operations.Domain.Tickets;

// =========================================================
// FILE: OperationsTicketMessage.cs
//
// PURPOSE:
// One message of a ticket, from the customer or from staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - The words, who wrote them and when
//
// IMPORTANT NOTES:
// - Kept inside its ticket; staff messages carry the staff member, never shown to the customer
// =========================================================
public sealed class OperationsTicketMessage
{
    private OperationsTicketMessage()
    {
        Text = string.Empty;
    }

    internal OperationsTicketMessage(bool fromStaff, Guid? userId, string text, DateTimeOffset sentUtc)
    {
        (FromStaff, UserId, Text, SentUtc) = (fromStaff, userId, text, sentUtc);
    }

    public bool FromStaff { get; private set; }

    public Guid? UserId { get; private set; }

    public string Text { get; private set; }

    public DateTimeOffset SentUtc { get; private set; }
}
