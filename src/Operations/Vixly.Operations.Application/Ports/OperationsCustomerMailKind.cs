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
    TicketAnswered = 14,

    // Slice 5F: finance found no Zelle or transfer for the customer's payment.
    PaymentNotReceived = 15,

    // Slice 5F step 2b (K-7): the monthly statement of a credit account is ready on the portal.
    CreditStatement = 16,

    // Slice 6C (E-5): a delay or an exception on one of the customer's shipments.
    ShipmentUpdate = 17,

    // Slice 7 (R-1): a shipment is ready for pickup at a destination warehouse.
    ReadyForPickup = 18,

    // Slice 7 step 2 (R-5, R-10): the pickup code, and the reminders of a shipment waiting at its destination.
    PickupCode = 19,
    PickupReminder = 20,
    PickupFinalNotice = 21,

    // Slice 7 step 3 (R-7, R-15): out for delivery today, with the delivery code when the company asks for one.
    OutForDelivery = 22,
    OutForDeliveryCode = 23,

    // Review item 3: a work email tried to open a customer account.
    EmailNotAllowed = 24
}
