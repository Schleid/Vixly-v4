using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoice;

// =========================================================
// FILE: PayMyOperationsInvoiceResponse.cs
//
// PURPOSE:
// What to pay and how, or the payment the customer reported (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the details of the chosen method, or the payment number once reported
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PayMyOperationsInvoiceResponse
{
    private PayMyOperationsInvoiceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string InvoiceNumber { get; private init; } = string.Empty;

    // What to write in the Zelle memo or the transfer's reference: the invoice number.
    public string Memo { get; private init; } = string.Empty;

    public decimal Amount { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    // Zelle: the company's terms; bank transfer: the company's instructions.
    public string? Text { get; private init; }

    public string? Tag { get; private init; }

    public string? Contact { get; private init; }

    public string? Name { get; private init; }

    public bool HasQr { get; private init; }

    public string? PaymentNumber { get; private init; }

    public static PayMyOperationsInvoiceResponse Success(string invoiceNumber, decimal amount, string currencyCode, string? text, string? tag, string? contact, string? name,
        bool hasQr) =>
        new()
        {
            Succeeded = true, InvoiceNumber = invoiceNumber, Memo = invoiceNumber, Amount = amount, CurrencyCode = currencyCode, Text = text, Tag = tag, Contact = contact,
            Name = name, HasQr = hasQr
        };

    public static PayMyOperationsInvoiceResponse Reported(string invoiceNumber, string paymentNumber) =>
        new() { Succeeded = true, InvoiceNumber = invoiceNumber, Memo = invoiceNumber, PaymentNumber = paymentNumber };

    public static PayMyOperationsInvoiceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new PayMyOperationsInvoiceResponse { Error = error };
    }
}
