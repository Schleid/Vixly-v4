namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCustomerMailKind.cs
//
// PURPOSE:
// The emails a customer receives about their account (Slice 2).
//
// RESPONSIBILITIES:
// - Name each email
//
// IMPORTANT NOTES:
// - Text in OperationsText: email.customer.{kind}.*
// =========================================================
public enum OperationsCustomerMailKind
{
    Verification = 1,
    PasswordReset = 2,
    AccountExists = 4,

    // Slice 3B: a package arrived; a package needs the customer's answer.
    ParcelReceived = 10,

    ParcelActionNeeded = 11,

    // Slice 3E: a package still waits for the customer (day 7, 14, 21); the final notice (day 25).
    ParcelReminder = 12,

    ParcelFinalNotice = 13,

    // Slice 4D step 4: staff answered or resolved the customer's ticket.
    TicketAnswered = 14
}
