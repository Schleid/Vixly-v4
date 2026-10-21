using Vixly.Operations.Application.Ports;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: SmtpOperationsCustomerMailer.cs
//
// PURPOSE:
// Sends customer account emails in the tenant brand and the customer language (Slice 2).
//
// RESPONSIBILITIES:
// - Pick the text of each kind; fill the name, company and link
//
// IMPORTANT NOTES:
// - Texts: email.customer.{kind}.subject and .body in OperationsText; never "Vixly"
// =========================================================
public sealed class SmtpOperationsCustomerMailer : IOperationsCustomerMailer
{
    private readonly OperationsMailSender _sender;

    public SmtpOperationsCustomerMailer(OperationsMailSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public Task SendAsync(OperationsCustomerMail mail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mail);

        string kind = mail.Kind switch
        {
            OperationsCustomerMailKind.Verification => "verification",
            OperationsCustomerMailKind.AccountExists => "account_exists",
            OperationsCustomerMailKind.ParcelReceived => "parcel_received",
            OperationsCustomerMailKind.ParcelActionNeeded => "parcel_action_needed",
            OperationsCustomerMailKind.ParcelReminder => "parcel_reminder",
            OperationsCustomerMailKind.ParcelFinalNotice => "parcel_final_notice",
            OperationsCustomerMailKind.TicketAnswered => "ticket_answered",
            OperationsCustomerMailKind.PaymentNotReceived => "payment_not_received",
            OperationsCustomerMailKind.CreditStatement => "credit_statement",
            OperationsCustomerMailKind.ShipmentUpdate => "shipment_update",
            OperationsCustomerMailKind.ReadyForPickup => "ready_for_pickup",
            OperationsCustomerMailKind.PickupCode => "pickup_code",
            OperationsCustomerMailKind.PickupReminder => "pickup_reminder",
            OperationsCustomerMailKind.PickupFinalNotice => "pickup_final_notice",
            _ => "password_reset"
        };
        Dictionary<string, object> values = new(StringComparer.Ordinal)
        {
            ["name"] = mail.ToName,
            ["company"] = mail.CompanyName,
            ["link"] = mail.Link
        };

        return _sender.SendAsync(
            mail.ToAddress,
            mail.ToName,
            mail.CompanyName,
            mail.Language,
            text =>
            {
                // Slice 6C: values as they are, and keys worded in the customer's language.
                Dictionary<string, object> all = new(values, StringComparer.Ordinal);

                foreach ((string name, string value) in mail.Extra)
                {
                    all[name] = value;
                }

                foreach ((string name, string key) in mail.Worded)
                {
                    all[name] = text[key].Value;
                }

                return (VixlyText.Format(text, "email.customer." + kind + ".subject", all), VixlyText.Format(text, "email.customer." + kind + ".body", all));
            },
            cancellationToken);
    }
}
