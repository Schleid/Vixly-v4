namespace Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoiceByCard;

// =========================================================
// FILE: PayMyOperationsInvoiceByCardRequest.cs
//
// PURPOSE:
// A customer pays one of their invoices by card on the portal, on Stripe's payment page (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PayMyOperationsInvoiceByCardRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid InvoiceId { get; init; }

    // Where Stripe's page sends the customer back: this address followed by the charge's id.
    public string ReturnUrlPrefix { get; init; } = string.Empty;
}
