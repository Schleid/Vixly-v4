using Vixly.Contracts.Operations.Billing;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerPayMethods.cs
//
// PURPOSE:
// The ways a customer can pay one invoice on the portal now (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Zelle and bank transfer on an unpaid invoice; card also on one billed to the credit account
// - Only the methods the company offers on the portal
//
// IMPORTANT NOTES:
// - Order shown: Zelle, bank transfer, card
// - Operations checks again when the customer pays
// =========================================================
public static class CustomerPayMethods
{
    public const string Zelle = "zelle";

    public const string BankTransfer = "bank_transfer";

    public const string Card = "card";

    public static IReadOnlyList<string> For(CustomerInvoicesDto invoices, CustomerInvoiceDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        ArgumentNullException.ThrowIfNull(invoice);
        List<string> methods = new();

        if (invoice.Owed <= 0m || invoice.Voided)
        {
            return methods;
        }

        if (invoices.ZelleOffered && invoice.State == "unpaid")
        {
            methods.Add(Zelle);
        }

        if (invoices.BankTransferOffered && invoice.State == "unpaid")
        {
            methods.Add(BankTransfer);
        }

        if (invoices.CardOffered && invoice.State is "unpaid" or "on_account")
        {
            methods.Add(Card);
        }

        return methods;
    }
}
