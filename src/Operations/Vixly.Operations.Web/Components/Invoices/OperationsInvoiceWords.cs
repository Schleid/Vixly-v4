using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Charges;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Web.Components.Invoices;

// =========================================================
// FILE: OperationsInvoiceWords.cs
//
// PURPOSE:
// Words of the invoice pages (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Name a line, the state of an invoice and an amount
//
// IMPORTANT NOTES:
// - Text comes from OperationsText (L-10); the PDF words the lines the same way
// =========================================================
public static class OperationsInvoiceWords
{
    public static string Line(IStringLocalizer<OperationsText> text, OperationsInvoiceLine line)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(line);

        return line.ChargeKind is { } kind
            ? OperationsChargeWords.Line(text, kind, line.Label, line.Piece, line.Quantity, line.Basis)
            : (line.Label ?? string.Empty) + (line.Code is { } code ? " (" + code + ")" : string.Empty);
    }

    // To pay, paid, credited, voided: the key of its state.
    public static string State(bool voided, decimal owed, decimal credited) =>
        voided ? "voided" : owed <= 0m && credited > 0m ? "credited" : owed <= 0m ? "settled" : credited > 0m ? "part_credited" : "to_pay";

    public static string State(OperationsInvoice invoice) =>
        State((invoice ?? throw new ArgumentNullException(nameof(invoice))).Status == OperationsInvoiceStatus.Voided, invoice.Owed, invoice.CreditedTotal);

    public static string Money(decimal amount, string currency) => OperationsChargeWords.Money(amount, currency);

    public static string Day(DateOnly day) => day.ToString("d", CultureInfo.CurrentCulture);
}
